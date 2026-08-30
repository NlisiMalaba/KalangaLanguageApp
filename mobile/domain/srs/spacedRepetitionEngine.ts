import { createSpacedRepetitionRecord } from '@/domain/srs/createSpacedRepetitionRecord';
import { recordAnswer } from '@/domain/srs/recordAnswer';
import type { DailyReviewInput, RecordSrsAnswerInput, SpacedRepetitionStore } from '@/domain/srs/types';
import type { SpacedRepetitionRecord } from '@/domain/entities';

export type SpacedRepetitionEngine = {
  recordAnswer(input: RecordSrsAnswerInput): Promise<SpacedRepetitionRecord>;
  getDailyReview(input: DailyReviewInput): Promise<SpacedRepetitionRecord[]>;
};

export function createSpacedRepetitionEngine(store: SpacedRepetitionStore): SpacedRepetitionEngine {
  return {
    async recordAnswer(input) {
      const now = input.now ?? new Date();
      const existing = await store.find(input);
      const current =
        existing ??
        createSpacedRepetitionRecord({
          languageId: input.languageId,
          userId: input.userId,
          phraseId: input.phraseId,
          variationId: input.variationId,
          now,
        });
      const updated = recordAnswer(current, input.isCorrect, now);
      await store.upsert(updated);
      return updated;
    },
    getDailyReview(input) {
      return store.listDue(input.languageId, input.userId, input.date);
    },
  };
}
