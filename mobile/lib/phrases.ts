import type { EntityId, Phrase } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';
import { mapPhrase, type PhraseRow } from '@/lib/mappers';

const UPSERT_SQL = `
INSERT INTO phrases (
  id, language_id, lesson_id, kalanga_text, english_translation, sort_order, created_at
) VALUES (?, ?, ?, ?, ?, ?, ?)
ON CONFLICT(id) DO UPDATE SET
  kalanga_text = excluded.kalanga_text,
  english_translation = excluded.english_translation,
  sort_order = excluded.sort_order
WHERE phrases.language_id = excluded.language_id
`;

export async function upsertPhrase(
  languageId: EntityId,
  phrase: Phrase,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, phrase.languageId);
  await withStore(store, (db) =>
    db.run(UPSERT_SQL, [
      phrase.id,
      tenant,
      phrase.lessonId,
      phrase.kalangaText,
      phrase.englishTranslation,
      phrase.sortOrder,
      phrase.createdAt,
    ]),
  );
}

export async function listPhrasesForLesson(
  languageId: EntityId,
  lessonId: EntityId,
  store?: LocalStore,
): Promise<Phrase[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<PhraseRow>(
      `SELECT * FROM phrases WHERE language_id = ? AND lesson_id = ? ORDER BY sort_order`,
      [tenant, lessonId],
    );
    return rows.map(mapPhrase);
  });
}
