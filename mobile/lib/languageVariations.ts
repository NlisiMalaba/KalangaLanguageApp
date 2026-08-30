import type { EntityId, LanguageVariation } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';
import { mapLanguageVariation, type LanguageVariationRow } from '@/lib/mappers';
import { sqlInPlaceholders } from '@/lib/sqlIn';

const UPSERT_SQL = `
INSERT INTO language_variations (
  id, language_id, phrase_id, kalanga_text, register_label, created_at
) VALUES (?, ?, ?, ?, ?, ?)
ON CONFLICT(id) DO UPDATE SET
  kalanga_text = excluded.kalanga_text,
  register_label = excluded.register_label
WHERE language_variations.language_id = excluded.language_id
`;

export async function upsertLanguageVariation(
  languageId: EntityId,
  variation: LanguageVariation,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, variation.languageId);
  await withStore(store, (db) =>
    db.run(UPSERT_SQL, [
      variation.id,
      tenant,
      variation.phraseId,
      variation.kalangaText,
      variation.registerLabel,
      variation.createdAt,
    ]),
  );
}

export async function getLanguageVariation(
  languageId: EntityId,
  variationId: EntityId,
  store?: LocalStore,
): Promise<LanguageVariation | null> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const row = await db.getFirst<LanguageVariationRow>(
      `SELECT * FROM language_variations WHERE language_id = ? AND id = ?`,
      [tenant, variationId],
    );
    return row ? mapLanguageVariation(row) : null;
  });
}

export async function listVariationsForPhrase(
  languageId: EntityId,
  phraseId: EntityId,
  store?: LocalStore,
): Promise<LanguageVariation[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<LanguageVariationRow>(
      `SELECT * FROM language_variations WHERE language_id = ? AND phrase_id = ?`,
      [tenant, phraseId],
    );
    return rows.map(mapLanguageVariation);
  });
}

export async function listVariationsForPhrases(
  languageId: EntityId,
  phraseIds: readonly EntityId[],
  store?: LocalStore,
): Promise<LanguageVariation[]> {
  if (phraseIds.length === 0) {
    return [];
  }

  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<LanguageVariationRow>(
      `SELECT * FROM language_variations WHERE language_id = ? AND phrase_id IN (${sqlInPlaceholders(phraseIds.length)})`,
      [tenant, ...phraseIds],
    );
    return rows.map(mapLanguageVariation);
  });
}
