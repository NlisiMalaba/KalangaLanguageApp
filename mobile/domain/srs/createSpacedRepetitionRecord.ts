import {
  DEFAULT_EASE_FACTOR,
  DEFAULT_SRS_INTERVAL_DAYS,
} from '@/constants/srs';
import type { EntityId, SpacedRepetitionRecord } from '@/domain/entities';
import { utcCalendarDate } from '@/domain/srs/calendarDate';

function newId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `srs-${Date.now()}`;
}

export function createSpacedRepetitionRecord(input: {
  languageId: EntityId;
  userId: EntityId;
  phraseId: EntityId;
  variationId?: EntityId | null;
  now: Date;
}): SpacedRepetitionRecord {
  const today = utcCalendarDate(input.now);
  const instant = input.now.toISOString();
  return {
    id: newId(),
    languageId: input.languageId,
    userId: input.userId,
    phraseId: input.phraseId,
    variationId: input.variationId ?? null,
    easeFactor: DEFAULT_EASE_FACTOR,
    intervalDays: DEFAULT_SRS_INTERVAL_DAYS,
    repetitions: 0,
    nextReviewAt: today,
    lastReviewedAt: null,
    updatedAt: instant,
  };
}
