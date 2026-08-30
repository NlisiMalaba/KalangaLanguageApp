import type { CalendarDate, EntityId, SpacedRepetitionRecord } from '@/domain/entities';
import type { SpacedRepetitionLookup, SpacedRepetitionStore } from '@/domain/srs/types';
import {
  findSpacedRepetition,
  listDueSpacedRepetition,
  upsertSpacedRepetition,
} from '@/lib/spacedRepetition';
import type { LocalStore } from '@/lib/localStore';

export function createSqliteSpacedRepetitionStore(store?: LocalStore): SpacedRepetitionStore {
  return {
    find(lookup: SpacedRepetitionLookup) {
      return findSpacedRepetition(
        lookup.languageId,
        lookup.userId,
        lookup.phraseId,
        lookup.variationId,
        store,
      );
    },
    upsert(record: SpacedRepetitionRecord) {
      return upsertSpacedRepetition(record.languageId, record, store);
    },
    listDue(languageId: EntityId, userId: EntityId, onOrBefore: CalendarDate) {
      return listDueSpacedRepetition(languageId, userId, onOrBefore, store);
    },
  };
}
