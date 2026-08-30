import * as fc from 'fast-check';

import { createMemoryCompleteLesson } from '@/domain/progress/completeLessonTestHarness';

const sampleArb = fc.record({
  score: fc.integer({ min: 0, max: 100 }),
  retryScore: fc.integer({ min: 0, max: 100 }),
  xpReward: fc.integer({ min: 1, max: 50 }),
});

describe('lesson completion records progress and awards XP', () => {
  // Feature: kalanga-language-app, Property 8: Lesson Completion Records Progress and Awards XP
  it('persists completion and awards XP once, including on retry', async () => {
    await fc.assert(
      fc.asyncProperty(sampleArb, async ({ score, retryScore, xpReward }) => {
        const { deps, complete } = createMemoryCompleteLesson();
        const now = new Date('2026-08-22T12:00:00.000Z');
        const input = {
          languageId: 'lang-1',
          userId: 'user-1',
          lessonId: 'lesson-1',
          score,
          xpReward,
          now,
        };

        const first = await complete(input);
        expect(first.xpGranted).toBe(true);
        expect(first.score).toBe(score);
        expect(first.xpAwarded).toBe(xpReward);
        expect(first.totalXp).toBe(xpReward);

        const persisted = deps.progress.find((row) => row.lessonId === 'lesson-1');
        expect(persisted).toBeDefined();
        expect(persisted?.completedAt).toBe(now.toISOString());
        expect(persisted?.score).toBe(score);
        expect(persisted?.xpAwarded).toBe(xpReward);
        expect(deps.gamification[0]?.totalXp).toBe(xpReward);
        expect(deps.sync.map((item) => item.entityType)).toEqual(['progress', 'gamification']);

        const second = await complete({
          ...input,
          score: retryScore,
          now: new Date('2026-08-22T18:00:00.000Z'),
        });
        expect(second.xpGranted).toBe(false);
        expect(second.score).toBe(retryScore);
        expect(second.xpAwarded).toBe(xpReward);
        expect(second.totalXp).toBe(xpReward);
        expect(deps.gamification[0]?.totalXp).toBe(xpReward);
      }),
      { numRuns: 100 },
    );
  });
});
