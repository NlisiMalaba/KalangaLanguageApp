import { createCompleteLessonUseCase } from '@/domain/progress/completeLessonUseCase';
import type { CompleteLessonDeps, EnqueueSyncItem } from '@/domain/progress/completeLessonUseCase';
import type { LearnerGamification, LearnerProgress } from '@/domain/entities';

export function memoryCompleteLessonDeps(): CompleteLessonDeps & {
  progress: LearnerProgress[];
  gamification: LearnerGamification[];
  sync: EnqueueSyncItem[];
} {
  const progress: LearnerProgress[] = [];
  const gamification: LearnerGamification[] = [];
  const sync: EnqueueSyncItem[] = [];

  return {
    progress,
    gamification,
    sync,
    async getProgress(_languageId, userId, lessonId) {
      return progress.find((row) => row.userId === userId && row.lessonId === lessonId) ?? null;
    },
    async upsertProgress(row) {
      const index = progress.findIndex((item) => item.userId === row.userId && item.lessonId === row.lessonId);
      if (index >= 0) {
        progress[index] = row;
        return;
      }

      progress.push(row);
    },
    async getGamification(_languageId, userId) {
      return gamification.find((row) => row.userId === userId) ?? null;
    },
    async upsertGamification(row) {
      const index = gamification.findIndex((item) => item.userId === row.userId);
      if (index >= 0) {
        gamification[index] = row;
        return;
      }

      gamification.push(row);
    },
    async enqueueSync(item) {
      sync.push(item);
    },
  };
}

export function createMemoryCompleteLesson() {
  const deps = memoryCompleteLessonDeps();
  return { deps, complete: createCompleteLessonUseCase(deps) };
}

export function utcNoon(isoDate: string): Date {
  return new Date(`${isoDate}T12:00:00.000Z`);
}
