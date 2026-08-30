import * as fc from 'fast-check';

import { WEAK_AREA_SCORE_THRESHOLD } from '@/constants/progress';
import { Level } from '@/domain/enums';
import { identifyWeakAreas, roundToOneDecimal } from '@/domain/progress/summarizeProgress';
import type { ProgressCompletion, ProgressLessonSummary } from '@/domain/progress/types';

const categories = ['Everyday', 'Travel', 'Family'] as const;

const sampleArb = fc.record({
  level: fc.constantFrom(Level.Beginner, Level.Intermediate, Level.Advanced),
  category: fc.constantFrom(...categories),
  scores: fc.array(fc.integer({ min: 0, max: 100 }), { minLength: 1, maxLength: 4 }),
});

describe('weak area identification below threshold', () => {
  // Feature: kalanga-language-app, Property 20: Weak Area Identification Below Threshold
  it('includes exactly the buckets whose average score is below the threshold', () => {
    fc.assert(
      fc.property(sampleArb, ({ level, category, scores }) => {
        const published: ProgressLessonSummary[] = [
          { id: 'strong-control', level, category: 'Work' },
          ...scores.map((_, index) => ({ id: `target-${index}`, level, category })),
        ];
        const completions: ProgressCompletion[] = [
          {
            lessonId: 'strong-control',
            completedAt: '2026-08-22T12:00:00.000Z',
            score: WEAK_AREA_SCORE_THRESHOLD,
          },
          ...scores.map((score, index) => ({
            lessonId: `target-${index}`,
            completedAt: '2026-08-22T12:00:00.000Z',
            score,
          })),
        ];

        const expectedAverage = roundToOneDecimal(
          scores.reduce((sum, score) => sum + score, 0) / scores.length,
        );
        const areas = identifyWeakAreas(published, completions);

        expect(areas.some((area) => area.level === level && area.category === 'Work')).toBe(false);

        const match = areas.find((area) => area.level === level && area.category === category);
        if (expectedAverage < WEAK_AREA_SCORE_THRESHOLD) {
          expect(match).toEqual({
            level,
            category,
            averageScore: expectedAverage,
            sampleSize: scores.length,
          });
        } else {
          expect(match).toBeUndefined();
        }
      }),
      { numRuns: 100 },
    );
  });
});
