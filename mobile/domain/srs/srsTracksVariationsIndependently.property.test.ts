import * as fc from 'fast-check';

import { createSpacedRepetitionEngine } from '@/domain/srs/spacedRepetitionEngine';
import type { SpacedRepetitionRecord } from '@/domain/entities';
import type { SpacedRepetitionStore } from '@/domain/srs/types';

const now = new Date('2026-08-22T12:00:00.000Z');

function memoryStore(): SpacedRepetitionStore & { rows: SpacedRepetitionRecord[] } {
  const rows: SpacedRepetitionRecord[] = [];
  const key = (row: { userId: string; phraseId: string; variationId: string | null }) =>
    `${row.userId}:${row.phraseId}:${row.variationId ?? ''}`;

  return {
    rows,
    async find(lookup) {
      const match = key({
        userId: lookup.userId,
        phraseId: lookup.phraseId,
        variationId: lookup.variationId ?? null,
      });
      return rows.find((row) => key(row) === match) ?? null;
    },
    async upsert(record) {
      const match = key(record);
      const index = rows.findIndex((row) => key(row) === match);
      if (index >= 0) {
        rows[index] = record;
        return;
      }

      rows.push(record);
    },
    async listDue() {
      return rows;
    },
  };
}

describe('SRS tracks language variations independently', () => {
  // Feature: kalanga-language-app, Property 38: SRS Tracks Language Variations Independently
  it('keeps base-phrase and variation schedules on separate cards', async () => {
    await fc.assert(
      fc.asyncProperty(fc.boolean(), fc.boolean(), async (baseCorrect, variationCorrect) => {
        const store = memoryStore();
        const engine = createSpacedRepetitionEngine(store);
        const lookup = { languageId: 'lang-1', userId: 'user-1', phraseId: 'phrase-1' };

        const base = await engine.recordAnswer({ ...lookup, isCorrect: baseCorrect, now });
        const variation = await engine.recordAnswer({
          ...lookup,
          variationId: 'var-1',
          isCorrect: variationCorrect,
          now,
        });

        expect(base.id).not.toBe(variation.id);
        expect(base.variationId).toBeNull();
        expect(variation.variationId).toBe('var-1');

        const intervalBefore = variation.intervalDays;
        const easeBefore = variation.easeFactor;
        await engine.recordAnswer({ ...lookup, isCorrect: true, now });

        const variationAfter = await store.find({ ...lookup, variationId: 'var-1' });
        expect(variationAfter?.intervalDays).toBe(intervalBefore);
        expect(variationAfter?.easeFactor).toBe(easeBefore);
      }),
      { numRuns: 100 },
    );
  });
});
