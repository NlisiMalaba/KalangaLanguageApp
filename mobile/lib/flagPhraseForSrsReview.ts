import { DEFAULT_SRS_EASE_FACTOR, DEFAULT_SRS_INTERVAL_DAYS } from '@/constants/exercises';
import type { FlagPhraseForReview } from '@/domain/exercises/types';
import { findSpacedRepetitionForPhrase, upsertSpacedRepetition } from '@/lib/spacedRepetition';
import type { LocalStore } from '@/lib/localStore';

function newId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `srs-${Date.now()}`;
}

function todayUtc(now: Date): string {
  return now.toISOString().slice(0, 10);
}

export function createSqlitePhraseReviewFlagger(
  store?: LocalStore,
  now: () => Date = () => new Date(),
): FlagPhraseForReview {
  return async ({ languageId, userId, phraseId }) => {
    const instant = now();
    const existing = await findSpacedRepetitionForPhrase(languageId, userId, phraseId, store);
    await upsertSpacedRepetition(
      languageId,
      {
        id: existing?.id ?? newId(),
        languageId,
        userId,
        phraseId,
        variationId: null,
        easeFactor: existing?.easeFactor ?? DEFAULT_SRS_EASE_FACTOR,
        intervalDays: DEFAULT_SRS_INTERVAL_DAYS,
        repetitions: 0,
        nextReviewAt: todayUtc(instant),
        lastReviewedAt: instant.toISOString(),
        updatedAt: instant.toISOString(),
      },
      store,
    );
  };
}
