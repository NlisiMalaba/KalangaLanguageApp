import type { EntityId, SpacedRepetitionRecord } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';
import { mapSpacedRepetition, type SpacedRepetitionRow } from '@/lib/mappers';

const UPSERT_SQL = `
INSERT INTO spaced_repetition_records (
  id, language_id, user_id, phrase_id, variation_id, ease_factor, interval_days,
  repetitions, next_review_at, last_reviewed_at, updated_at
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
ON CONFLICT(id) DO UPDATE SET
  ease_factor = excluded.ease_factor,
  interval_days = excluded.interval_days,
  repetitions = excluded.repetitions,
  next_review_at = excluded.next_review_at,
  last_reviewed_at = excluded.last_reviewed_at,
  updated_at = excluded.updated_at
WHERE spaced_repetition_records.language_id = excluded.language_id
  AND excluded.updated_at >= spaced_repetition_records.updated_at
`;

export async function upsertSpacedRepetition(
  languageId: EntityId,
  record: SpacedRepetitionRecord,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, record.languageId);
  await withStore(store, (db) =>
    db.run(UPSERT_SQL, [
      record.id,
      tenant,
      record.userId,
      record.phraseId,
      record.variationId,
      record.easeFactor,
      record.intervalDays,
      record.repetitions,
      record.nextReviewAt,
      record.lastReviewedAt,
      record.updatedAt,
    ]),
  );
}

export async function listDueSpacedRepetition(
  languageId: EntityId,
  userId: EntityId,
  onOrBefore: string,
  store?: LocalStore,
): Promise<SpacedRepetitionRecord[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<SpacedRepetitionRow>(
      `SELECT * FROM spaced_repetition_records
       WHERE language_id = ? AND user_id = ? AND next_review_at <= ?
       ORDER BY next_review_at`,
      [tenant, userId, onOrBefore],
    );
    return rows.map(mapSpacedRepetition);
  });
}
