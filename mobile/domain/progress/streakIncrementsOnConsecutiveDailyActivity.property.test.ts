import * as fc from 'fast-check';

import { addCalendarDays } from '@/domain/srs/calendarDate';
import { createMemoryCompleteLesson, utcNoon } from '@/domain/progress/completeLessonTestHarness';

const sampleArb = fc.tuple(fc.integer({ min: 0, max: 40 }), fc.integer({ min: 2, max: 10 }));

describe('streak increments on consecutive daily activity', () => {
  // Feature: kalanga-language-app, Property 17: Streak Increments on Consecutive Daily Activity
  it('increments current and longest streak by one each consecutive calendar day', async () => {
    await fc.assert(
      fc.asyncProperty(sampleArb, async ([startOffset, days]) => {
        const { deps, complete } = createMemoryCompleteLesson();
        const start = addCalendarDays('2026-01-01', startOffset);
        const base = { languageId: 'lang-1', userId: 'user-1', xpReward: 5 };

        let lastStreak = 0;
        for (let i = 0; i < days; i += 1) {
          const day = addCalendarDays(start, i);
          const result = await complete({
            ...base,
            lessonId: `lesson-${i}`,
            score: 80,
            now: utcNoon(day),
          });
          expect(result.currentStreak).toBe(i + 1);
          expect(deps.gamification[0]?.longestStreak).toBe(i + 1);
          expect(deps.gamification[0]?.lastActivityDate).toBe(day);
          lastStreak = result.currentStreak;
        }

        const sameDay = await complete({
          ...base,
          lessonId: `lesson-same-${days}`,
          score: 70,
          now: utcNoon(addCalendarDays(start, days - 1)),
        });
        expect(sameDay.currentStreak).toBe(days);
        expect(sameDay.currentStreak).toBe(lastStreak);
        expect(deps.gamification[0]?.longestStreak).toBe(days);
      }),
      { numRuns: 100 },
    );
  });
});
