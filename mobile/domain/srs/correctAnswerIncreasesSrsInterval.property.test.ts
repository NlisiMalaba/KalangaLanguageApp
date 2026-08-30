import * as fc from 'fast-check';

import { addCalendarDays, utcCalendarDate } from '@/domain/srs/calendarDate';
import { createSpacedRepetitionRecord } from '@/domain/srs/createSpacedRepetitionRecord';
import { recordAnswer } from '@/domain/srs/recordAnswer';
import type { SpacedRepetitionRecord } from '@/domain/entities';

const now = new Date('2026-08-22T12:00:00.000Z');
const today = utcCalendarDate(now);

const matureCardArb: fc.Arbitrary<SpacedRepetitionRecord> = fc
  .tuple(fc.integer({ min: 130, max: 350 }), fc.integer({ min: 1, max: 40 }), fc.integer({ min: 2, max: 8 }))
  .map(([easeCents, intervalDays, repetitions]) => ({
    ...createSpacedRepetitionRecord({
      languageId: 'lang-1',
      userId: 'user-1',
      phraseId: 'phrase-1',
      now,
    }),
    easeFactor: easeCents / 100,
    intervalDays,
    repetitions,
    nextReviewAt: addCalendarDays(today, intervalDays),
  }));

describe('correct answer increases SRS interval', () => {
  // Feature: kalanga-language-app, Property 15: Correct Answer Increases SRS Interval
  it('lengthens the interval and delays the next review for a mature card', () => {
    fc.assert(
      fc.property(matureCardArb, (card) => {
        const updated = recordAnswer(card, true, now);
        expect(updated.intervalDays).toBeGreaterThan(card.intervalDays);
        expect(updated.repetitions).toBe(card.repetitions + 1);
        expect(updated.nextReviewAt > today).toBe(true);
        expect(updated.nextReviewAt).toBe(addCalendarDays(today, updated.intervalDays));
        expect(updated.easeFactor).toBeGreaterThanOrEqual(card.easeFactor);
      }),
      { numRuns: 100 },
    );
  });
});
