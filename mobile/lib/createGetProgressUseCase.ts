import { createGetProgressUseCase } from '@/domain/progress/getProgressUseCase';
import type { GetProgressDeps } from '@/domain/progress/getProgressUseCase';
import { listExerciseScoresForUser } from '@/lib/exerciseResults';
import { getGamification } from '@/lib/gamification';
import { listLessonProgressForUser } from '@/lib/lessonProgress';
import { listPublishedLessons } from '@/lib/lessons';
import type { LocalStore } from '@/lib/localStore';

export function createDefaultGetProgressUseCase(
  overrides: Partial<GetProgressDeps> & { store?: LocalStore } = {},
) {
  const store = overrides.store;
  const deps: GetProgressDeps = {
    listPublishedLessons: async (languageId) => {
      const lessons = await listPublishedLessons(languageId, store);
      return lessons.map((lesson) => ({
        id: lesson.id,
        level: lesson.level,
        category: lesson.category,
      }));
    },
    listProgress: (languageId, userId) => listLessonProgressForUser(languageId, userId, store),
    listExerciseScores: (languageId, userId) => listExerciseScoresForUser(languageId, userId, store),
    getGamification: (languageId, userId) => getGamification(languageId, userId, store),
    ...overrides,
  };

  return createGetProgressUseCase(deps);
}
