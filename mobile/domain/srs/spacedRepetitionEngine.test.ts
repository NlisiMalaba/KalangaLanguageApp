import { createSpacedRepetitionEngine } from '@/domain/srs/spacedRepetitionEngine';
import type { SpacedRepetitionRecord } from '@/domain/entities';
import type { SpacedRepetitionStore } from '@/domain/srs/types';

function memoryStore(seed: SpacedRepetitionRecord[] = []): SpacedRepetitionStore & { rows: SpacedRepetitionRecord[] } {
  const rows = [...seed];
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
    async listDue(_languageId, userId, onOrBefore) {
      return rows.filter((row) => row.userId === userId && row.nextReviewAt <= onOrBefore);
    },
  };
}

const now = new Date('2026-08-22T12:00:00.000Z');

describe('SpacedRepetitionEngine', () => {
  it('returns only cards due on or before the review date', async () => {
    const store = memoryStore([
      {
        id: 'due',
        languageId: 'lang-1',
        userId: 'user-1',
        phraseId: 'phrase-due',
        variationId: null,
        easeFactor: 2.5,
        intervalDays: 1,
        repetitions: 1,
        nextReviewAt: '2026-08-22',
        lastReviewedAt: null,
        updatedAt: now.toISOString(),
      },
      {
        id: 'later',
        languageId: 'lang-1',
        userId: 'user-1',
        phraseId: 'phrase-later',
        variationId: null,
        easeFactor: 2.5,
        intervalDays: 6,
        repetitions: 2,
        nextReviewAt: '2026-08-28',
        lastReviewedAt: null,
        updatedAt: now.toISOString(),
      },
      {
        id: 'other-user',
        languageId: 'lang-1',
        userId: 'user-2',
        phraseId: 'phrase-due',
        variationId: null,
        easeFactor: 2.5,
        intervalDays: 1,
        repetitions: 1,
        nextReviewAt: '2026-08-22',
        lastReviewedAt: null,
        updatedAt: now.toISOString(),
      },
    ]);
    const engine = createSpacedRepetitionEngine(store);
    const review = await engine.getDailyReview({
      languageId: 'lang-1',
      userId: 'user-1',
      date: '2026-08-22',
    });
    expect(review.map((row) => row.id)).toEqual(['due']);
  });

  it('schedules the base phrase and a variation independently', async () => {
    const store = memoryStore();
    const engine = createSpacedRepetitionEngine(store);
    const base = await engine.recordAnswer({
      languageId: 'lang-1',
      userId: 'user-1',
      phraseId: 'phrase-1',
      isCorrect: true,
      now,
    });
    const variation = await engine.recordAnswer({
      languageId: 'lang-1',
      userId: 'user-1',
      phraseId: 'phrase-1',
      variationId: 'var-1',
      isCorrect: false,
      now,
    });

    expect(base.id).not.toBe(variation.id);
    expect(base.variationId).toBeNull();
    expect(variation.variationId).toBe('var-1');
    expect(base.repetitions).toBe(1);
    expect(variation.repetitions).toBe(0);

    const intervalBefore = variation.intervalDays;
    await engine.recordAnswer({
      languageId: 'lang-1',
      userId: 'user-1',
      phraseId: 'phrase-1',
      isCorrect: true,
      now,
    });

    const variationAfter = await store.find({
      languageId: 'lang-1',
      userId: 'user-1',
      phraseId: 'phrase-1',
      variationId: 'var-1',
    });
    expect(variationAfter?.intervalDays).toBe(intervalBefore);
    expect(variationAfter?.easeFactor).toBe(variation.easeFactor);
  });
});
