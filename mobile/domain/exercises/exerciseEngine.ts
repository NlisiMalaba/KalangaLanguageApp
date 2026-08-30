import type { EntityId } from '@/domain/entities';
import { ExerciseStructureError } from '@/domain/exercises/errors';
import { gradeExercise } from '@/domain/exercises/gradeExercise';
import { parseExercisePrompt } from '@/domain/exercises/parseExercisePrompt';
import type { PreparedExercise } from '@/domain/exercises/prepareExercises';
import { prepareExercises } from '@/domain/exercises/prepareExercises';
import type {
  ExerciseAttempt,
  ExerciseEngineDeps,
  ExerciseSource,
  GradeResult,
} from '@/domain/exercises/types';

export type SubmitExerciseInput = {
  languageId: EntityId;
  userId: EntityId;
  exercise: ExerciseSource;
  attempt: ExerciseAttempt;
};

export type ExerciseEngine = {
  loadLessonExercises(languageId: EntityId, lessonId: EntityId): Promise<PreparedExercise[]>;
  submit(input: SubmitExerciseInput): Promise<GradeResult>;
};

export function createExerciseEngine(deps: ExerciseEngineDeps): ExerciseEngine {
  return {
    async loadLessonExercises(languageId, lessonId) {
      const rows = await deps.listExercises(languageId, lessonId);
      return prepareExercises(rows);
    },
    async submit({ languageId, userId, exercise, attempt }) {
      const parsed = parseExercisePrompt(exercise);
      if (!parsed) {
        throw new ExerciseStructureError();
      }

      const result = gradeExercise(parsed, attempt);
      if (!result.correct && result.phraseId) {
        await deps.flagPhraseForReview({ languageId, userId, phraseId: result.phraseId });
      }

      await deps.recordExerciseResult?.({
        languageId,
        userId,
        exerciseId: exercise.id,
        lessonId: exercise.lessonId,
        isCorrect: result.correct,
        score: result.correct ? 100 : 0,
        answeredAt: new Date().toISOString(),
      });

      return result;
    },
  };
}
