import { createExerciseEngine } from '@/domain/exercises/exerciseEngine';
import type { ExerciseEngineDeps } from '@/domain/exercises/types';
import { listExercisesForLesson } from '@/lib/exercises';
import { insertExerciseResult } from '@/lib/exerciseResults';
import { createSqlitePhraseReviewFlagger } from '@/lib/flagPhraseForSrsReview';
import type { LocalStore } from '@/lib/localStore';

export function createDefaultExerciseEngine(
  overrides: Partial<ExerciseEngineDeps> & { store?: LocalStore } = {},
) {
  const store = overrides.store;
  return createExerciseEngine({
    listExercises: (languageId, lessonId) => listExercisesForLesson(languageId, lessonId, store),
    flagPhraseForReview: createSqlitePhraseReviewFlagger(store),
    recordExerciseResult: (input) => insertExerciseResult(input, store),
    ...overrides,
  });
}
