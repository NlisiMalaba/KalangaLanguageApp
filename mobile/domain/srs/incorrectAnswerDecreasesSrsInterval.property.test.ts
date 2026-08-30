import * as fc from 'fast-check';

import { DEFAULT_SRS_INTERVAL_DAYS } from '@/constants/srs';
import { addCalendarDays, utcCalendarDate } from '@/domain/srs/calendarDate';
import { createSpacedRepetitionRecord } from '@/domain/srs/createSpacedRepetitionRecord';
import { recordAnswer } from '@/domain/srs/recordAnswer';
import type { SpacedRepetitionRecord } from '@/domain/entities';

const now = new Date('2026-08-22T12:00:00.000Z');
const today = utcCalendarDate(now);

const reviewedCardArb: fc.Arbitrary<SpacedRepetitionRecord> = fc
  .tuple(fc.integer({ min: 130, max: 350 }), fc.integer({ min: 2, max: 40 }), fc.integer({ min: 1, max: 8 }))
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

describe('incorrect answer decreases SRS interval', () => {
  // Feature: kalanga-language-app, Property 14: Incorrect Answer Decreases SRS Interval
  it('resets repetitions and schedules the next review sooner', () => {
    fc.assert(
      fc.property(reviewedCardArb, (card) => {
        const previousNext = card.nextReviewAt;
        const updated = recordAnswer(card, false, now);
        expect(updated.repetitions).toBe(0);
        expect(updated.intervalDays).toBe(DEFAULT_SRS_INTERVAL_DAYS);
        expect(updated.intervalDays).toBeLessThan(card.intervalDays);
        expect(updated.nextReviewAt < previousNext).toBe(true);
        expect(updated.nextReviewAt).toBe(addCalendarDays(today, updated.intervalDays));
        expect(updated.easeFactor).toBeLessThanOrEqual(card.easeFactor);
      }),
      { numRuns: 100 },
    );
  });
});
