import { createCompleteLessonUseCase } from '@/domain/progress/completeLessonUseCase';
import type { CompleteLessonDeps } from '@/domain/progress/completeLessonUseCase';
import { getGamification, upsertGamification } from '@/lib/gamification';
import { getLessonProgress, upsertLessonProgress } from '@/lib/lessonProgress';
import { enqueueSync, type SyncQueueEntityType } from '@/lib/syncQueue';
import type { LocalStore } from '@/lib/localStore';

export function createDefaultCompleteLessonUseCase(
  overrides: Partial<CompleteLessonDeps> & { store?: LocalStore } = {},
) {
  const store = overrides.store;
  const deps: CompleteLessonDeps = {
    getProgress: (languageId, userId, lessonId) => getLessonProgress(languageId, userId, lessonId, store),
    upsertProgress: (progress) => upsertLessonProgress(progress.languageId, progress, store),
    getGamification: (languageId, userId) => getGamification(languageId, userId, store),
    upsertGamification: (gamification) => upsertGamification(gamification.languageId, gamification, store),
    enqueueSync: (item) =>
      enqueueSync(
        item.languageId,
        {
          id: item.id,
          languageId: item.languageId,
          userId: item.userId,
          clientOperationId: item.clientOperationId,
          entityType: item.entityType as SyncQueueEntityType,
          payload: item.payload,
          createdAt: item.createdAt,
          updatedAt: item.updatedAt,
        },
        store,
      ),
    ...overrides,
  };

  return createCompleteLessonUseCase(deps);
}
