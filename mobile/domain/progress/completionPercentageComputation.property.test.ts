import * as fc from 'fast-check';

import { Level } from '@/domain/enums';
import { bucketsByCategory, bucketsByLevel, roundToOneDecimal } from '@/domain/progress/summarizeProgress';
import type { ProgressCompletion, ProgressLessonSummary } from '@/domain/progress/types';

const categories = ['Everyday', 'Travel', 'Family'] as const;

const sampleArb = fc.record({
  level: fc.constantFrom(Level.Beginner, Level.Intermediate, Level.Advanced),
  category: fc.constantFrom(...categories),
  completeFlags: fc.array(fc.boolean(), { minLength: 1, maxLength: 6 }),
});

describe('completion percentage computation', () => {
  // Feature: kalanga-language-app, Property 21: Completion Percentage Computation
  it('is completed published lessons over published lessons in the bucket', () => {
    fc.assert(
      fc.property(sampleArb, ({ level, category, completeFlags }) => {
        const published: ProgressLessonSummary[] = completeFlags.map((_, index) => ({
          id: `lesson-${index}`,
          level,
          category,
        }));
        const completions: ProgressCompletion[] = completeFlags.flatMap((done, index) =>
          done
            ? [
                {
                  lessonId: `lesson-${index}`,
                  completedAt: '2026-08-22T12:00:00.000Z',
                  score: 80,
                },
              ]
            : [],
        );

        const completed = completeFlags.filter(Boolean).length;
        const total = completeFlags.length;
        const expected = roundToOneDecimal((100 * completed) / total);

        const byLevel = bucketsByLevel(published, completions).find((bucket) => bucket.level === level);
        expect(byLevel?.completedCount).toBe(completed);
        expect(byLevel?.totalCount).toBe(total);
        expect(byLevel?.percentage).toBe(expected);

        const byCategory = bucketsByCategory(published, completions).find((bucket) => bucket.category === category);
        expect(byCategory?.completedCount).toBe(completed);
        expect(byCategory?.totalCount).toBe(total);
        expect(byCategory?.percentage).toBe(expected);
      }),
      { numRuns: 100 },
    );
  });
});
