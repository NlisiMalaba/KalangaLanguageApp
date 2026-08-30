import type { CalendarDate, EntityId, SpacedRepetitionRecord } from '@/domain/entities';

export type SpacedRepetitionLookup = {
  languageId: EntityId;
  userId: EntityId;
  phraseId: EntityId;
  variationId?: EntityId | null;
};

export type SpacedRepetitionStore = {
  find(lookup: SpacedRepetitionLookup): Promise<SpacedRepetitionRecord | null>;
  upsert(record: SpacedRepetitionRecord): Promise<void>;
  listDue(languageId: EntityId, userId: EntityId, onOrBefore: CalendarDate): Promise<SpacedRepetitionRecord[]>;
};

export type RecordSrsAnswerInput = SpacedRepetitionLookup & {
  isCorrect: boolean;
  now?: Date;
};

export type DailyReviewInput = {
  languageId: EntityId;
  userId: EntityId;
  date: CalendarDate;
};
