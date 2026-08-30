import { parseExercisePrompt } from '@/domain/exercises/parseExercisePrompt';
import type { ExerciseSource, ParsedExercise } from '@/domain/exercises/types';
import type { LessonDetail } from '@/domain/catalog/types';

export type PreparedExercise = {
  source: ExerciseSource;
  parsed: ParsedExercise;
};

export function exerciseSourcesFromLesson(lesson: LessonDetail): ExerciseSource[] {
  return lesson.exercises.map((exercise) => ({
    id: exercise.id,
    languageId: lesson.languageId,
    lessonId: lesson.id,
    exerciseType: exercise.exerciseType,
    promptData: exercise.promptData,
    correctAnswer: exercise.correctAnswer,
    sortOrder: exercise.sortOrder,
  }));
}

export function prepareExercises(sources: readonly ExerciseSource[]): PreparedExercise[] {
  return [...sources]
    .sort((left, right) => left.sortOrder - right.sortOrder)
    .flatMap((source) => {
      const parsed = parseExercisePrompt(source);
      return parsed ? [{ source, parsed }] : [];
    });
}
