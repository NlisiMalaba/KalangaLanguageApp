import { createCompleteLessonUseCase } from '@/domain/progress/completeLessonUseCase';
import type { CompleteLessonDeps, EnqueueSyncItem } from '@/domain/progress/completeLessonUseCase';
import type { LearnerGamification, LearnerProgress } from '@/domain/entities';

function memoryDeps(): CompleteLessonDeps & {
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

describe('completeLessonUseCase', () => {
  const now = new Date('2026-08-22T12:00:00.000Z');

  it('records progress, awards XP once, updates streak, and enqueues sync', async () => {
    const deps = memoryDeps();
    const complete = createCompleteLessonUseCase(deps);
    const input = {
      languageId: 'lang-1',
      userId: 'user-1',
      lessonId: 'lesson-1',
      score: 80,
      xpReward: 10,
      now,
    };

    const first = await complete(input);
    expect(first.xpGranted).toBe(true);
    expect(first.xpAwarded).toBe(10);
    expect(first.totalXp).toBe(10);
    expect(first.currentStreak).toBe(1);
    expect(deps.progress[0]?.completedAt).toBe(now.toISOString());
    expect(deps.sync.map((item) => item.entityType)).toEqual(['progress', 'gamification']);

    const second = await complete({ ...input, score: 40, now: new Date('2026-08-22T18:00:00.000Z') });
    expect(second.xpGranted).toBe(false);
    expect(second.score).toBe(40);
    expect(second.xpAwarded).toBe(10);
    expect(second.totalXp).toBe(10);
    expect(deps.gamification[0]?.totalXp).toBe(10);
  });

  it('increments streak on the next calendar day and resets after a gap', async () => {
    const deps = memoryDeps();
    const complete = createCompleteLessonUseCase(deps);
    const base = { languageId: 'lang-1', userId: 'user-1', xpReward: 5 };

    await complete({ ...base, lessonId: 'a', score: 100, now: new Date('2026-08-22T12:00:00.000Z') });
    const nextDay = await complete({
      ...base,
      lessonId: 'b',
      score: 90,
      now: new Date('2026-08-23T12:00:00.000Z'),
    });
    expect(nextDay.currentStreak).toBe(2);
    expect(deps.gamification[0]?.longestStreak).toBe(2);

    const afterGap = await complete({
      ...base,
      lessonId: 'c',
      score: 70,
      now: new Date('2026-08-25T12:00:00.000Z'),
    });
    expect(afterGap.currentStreak).toBe(1);
  });
});
