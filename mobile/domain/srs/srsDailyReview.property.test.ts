import * as fc from 'fast-check';

import { addCalendarDays, utcCalendarDate } from '@/domain/srs/calendarDate';
import { createSpacedRepetitionEngine } from '@/domain/srs/spacedRepetitionEngine';
import type { SpacedRepetitionRecord } from '@/domain/entities';
import type { SpacedRepetitionStore } from '@/domain/srs/types';

const now = new Date('2026-08-22T12:00:00.000Z');
const today = utcCalendarDate(now);

function memoryStore(seed: SpacedRepetitionRecord[]): SpacedRepetitionStore {
  const rows = [...seed];
  return {
    async find() {
      return null;
    },
    async upsert(record) {
      rows.push(record);
    },
    async listDue(languageId, userId, onOrBefore) {
      return rows.filter(
        (row) =>
          row.languageId === languageId &&
          row.userId === userId &&
          row.nextReviewAt <= onOrBefore,
      );
    },
  };
}

const offsetsArb = fc.array(fc.integer({ min: -6, max: 8 }), { minLength: 1, maxLength: 6 });

describe('SRS daily review contains exactly due phrases', () => {
  // Feature: kalanga-language-app, Property 16: SRS Daily Review Contains Exactly Due Phrases
  it('returns only the current learner cards with next_review_at on or before the as-of date', async () => {
    await fc.assert(
      fc.asyncProperty(offsetsArb, async (offsets) => {
        const learner = 'user-1';
        const other = 'user-2';
        const languageId = 'lang-1';
        const expectedDue = new Set<string>();
        const rows: SpacedRepetitionRecord[] = offsets.map((offset, index) => {
          const phraseId = `phrase-${index}`;
          if (offset <= 0) {
            expectedDue.add(phraseId);
          }

          return {
            id: `srs-${index}`,
            languageId,
            userId: learner,
            phraseId,
            variationId: null,
            easeFactor: 2.5,
            intervalDays: 1,
            repetitions: 0,
            nextReviewAt: addCalendarDays(today, offset),
            lastReviewedAt: null,
            updatedAt: now.toISOString(),
          };
        });

        rows.push({
          id: 'other-due',
          languageId,
          userId: other,
          phraseId: 'phrase-0',
          variationId: null,
          easeFactor: 2.5,
          intervalDays: 1,
          repetitions: 0,
          nextReviewAt: addCalendarDays(today, -1),
          lastReviewedAt: null,
          updatedAt: now.toISOString(),
        });

        const engine = createSpacedRepetitionEngine(memoryStore(rows));
        const due = await engine.getDailyReview({ languageId, userId: learner, date: today });
        const duePhrases = new Set(due.map((row) => row.phraseId));

        expect(duePhrases).toEqual(expectedDue);
        for (const item of due) {
          expect(item.userId).toBe(learner);
          expect(item.nextReviewAt <= today).toBe(true);
        }
      }),
      { numRuns: 100 },
    );
  });
});
