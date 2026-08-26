import type { ContentPack, EntityId } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';
import { mapContentPack, type ContentPackRow } from '@/lib/mappers';

const UPSERT_SQL = `
INSERT INTO content_packs (
  id, language_id, name, level, category, version, size_bytes, manifest_url,
  lesson_ids, created_at, updated_at
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
ON CONFLICT(id) DO UPDATE SET
  name = excluded.name,
  level = excluded.level,
  category = excluded.category,
  version = excluded.version,
  size_bytes = excluded.size_bytes,
  manifest_url = excluded.manifest_url,
  lesson_ids = excluded.lesson_ids,
  updated_at = excluded.updated_at
WHERE content_packs.language_id = excluded.language_id
`;

export async function upsertContentPack(
  languageId: EntityId,
  pack: ContentPack,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, pack.languageId);
  await withStore(store, (db) =>
    db.run(UPSERT_SQL, [
      pack.id,
      tenant,
      pack.name,
      pack.level,
      pack.category,
      pack.version,
      pack.sizeBytes,
      pack.manifestUrl,
      JSON.stringify(pack.lessonIds),
      pack.createdAt,
      pack.updatedAt,
    ]),
  );
}

export async function listContentPacks(
  languageId: EntityId,
  store?: LocalStore,
): Promise<ContentPack[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<ContentPackRow>(
      `SELECT * FROM content_packs WHERE language_id = ? ORDER BY name`,
      [tenant],
    );
    return rows.map(mapContentPack);
  });
}

export async function deleteContentPack(
  languageId: EntityId,
  packId: EntityId,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireLanguageId(languageId);
  await withStore(store, async (db) => {
    await db.run(`DELETE FROM download_progress WHERE language_id = ? AND content_pack_id = ?`, [
      tenant,
      packId,
    ]);
    await db.run(`DELETE FROM content_packs WHERE language_id = ? AND id = ?`, [tenant, packId]);
  });
}
