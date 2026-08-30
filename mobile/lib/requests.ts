import type { EntityId, Request } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';
import { mapRequest, type RequestRow } from '@/lib/mappers';

const UPSERT_SQL = `
INSERT INTO requests (
  id, language_id, submitter_id, title, description, upvote_count, status,
  fulfilled_by_lesson_id, created_at, updated_at
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
ON CONFLICT(id) DO UPDATE SET
  title = excluded.title,
  description = excluded.description,
  upvote_count = excluded.upvote_count,
  status = excluded.status,
  fulfilled_by_lesson_id = excluded.fulfilled_by_lesson_id,
  updated_at = excluded.updated_at
WHERE requests.language_id = excluded.language_id
`;

export async function upsertRequest(
  languageId: EntityId,
  request: Request,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, request.languageId);
  await withStore(store, (db) =>
    db.run(UPSERT_SQL, [
      request.id,
      tenant,
      request.submitterId,
      request.title,
      request.description,
      request.upvoteCount,
      request.status,
      request.fulfilledByLessonId,
      request.createdAt,
      request.updatedAt,
    ]),
  );
}

export async function listRequestsByUpvotes(
  languageId: EntityId,
  store?: LocalStore,
): Promise<Request[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<RequestRow>(
      `SELECT * FROM requests WHERE language_id = ? ORDER BY upvote_count DESC, created_at DESC`,
      [tenant],
    );
    return rows.map(mapRequest);
  });
}
