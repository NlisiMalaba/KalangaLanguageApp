import { utcCalendarDate } from '@/domain/srs/calendarDate';
import type { SpacedRepetitionEngine } from '@/domain/srs/spacedRepetitionEngine';
import type { ReviewPrompt } from '@/domain/progress/dailyReview';
import type { EntityId, LanguageVariation, Phrase } from '@/domain/entities';
import type { LocalStore } from '@/lib/localStore';
import { getLanguageVariation } from '@/lib/languageVariations';
import { getPhrase } from '@/lib/phrases';

export function createLoadReviewPrompts(
  srs: SpacedRepetitionEngine,
  store?: LocalStore,
): (input: { languageId: EntityId; userId: EntityId; date?: string }) => Promise<ReviewPrompt[]> {
  return async ({ languageId, userId, date }) => {
    const asOf = date ?? utcCalendarDate(new Date());
    const due = await srs.getDailyReview({ languageId, userId, date: asOf });
    const prompts: ReviewPrompt[] = [];

    for (const card of due) {
      const phrase = await getPhrase(languageId, card.phraseId, store);
      if (!phrase) {
        continue;
      }

      let kalangaText = phrase.kalangaText;
      if (card.variationId) {
        const variation: LanguageVariation | null = await getLanguageVariation(
          languageId,
          card.variationId,
          store,
        );
        if (variation) {
          kalangaText = variation.kalangaText;
        }
      }

      prompts.push({ phrase, kalangaText, card });
    }

    return prompts;
  };
}
