import * as fc from 'fast-check';

import { addCalendarDays } from '@/domain/srs/calendarDate';
import { createMemoryCompleteLesson, utcNoon } from '@/domain/progress/completeLessonTestHarness';

const sampleArb = fc.tuple(fc.integer({ min: 1, max: 6 }), fc.integer({ min: 2, max: 6 }));

describe('streak resets on missed day', () => {
  // Feature: kalanga-language-app, Property 18: Streak Resets on Missed Day
  it('resets current streak after a gap of two or more calendar days', async () => {
    await fc.assert(
      fc.asyncProperty(sampleArb, async ([streakDays, gapDays]) => {
        const { deps, complete } = createMemoryCompleteLesson();
        const start = '2026-03-01';
        const base = { languageId: 'lang-1', userId: 'user-1', xpReward: 5 };

        for (let i = 0; i < streakDays; i += 1) {
          await complete({
            ...base,
            lessonId: `lesson-${i}`,
            score: 90,
            now: utcNoon(addCalendarDays(start, i)),
          });
        }

        const lastActivity = addCalendarDays(start, streakDays - 1);
        const afterGap = await complete({
          ...base,
          lessonId: 'lesson-after-gap',
          score: 60,
          now: utcNoon(addCalendarDays(lastActivity, gapDays)),
        });

        expect(afterGap.currentStreak).toBe(1);
        expect(deps.gamification[0]?.lastActivityDate).toBe(addCalendarDays(lastActivity, gapDays));
        expect(deps.gamification[0]?.longestStreak).toBe(streakDays);
      }),
      { numRuns: 100 },
    );
  });
});
