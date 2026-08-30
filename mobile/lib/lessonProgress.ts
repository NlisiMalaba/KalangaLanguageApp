import type { EntityId, LearnerProgress } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';
import { mapLearnerProgress, type LearnerProgressRow } from '@/lib/mappers';

const UPSERT_SQL = `
INSERT INTO learner_progress (
  id, language_id, user_id, lesson_id, completed_at, score, xp_awarded, updated_at
) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
ON CONFLICT(user_id, lesson_id) DO UPDATE SET
  completed_at = excluded.completed_at,
  score = excluded.score,
  xp_awarded = excluded.xp_awarded,
  updated_at = excluded.updated_at
WHERE learner_progress.language_id = excluded.language_id
  AND excluded.updated_at >= learner_progress.updated_at
`;

export async function upsertLessonProgress(
  languageId: EntityId,
  progress: LearnerProgress,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, progress.languageId);
  await withStore(store, (db) =>
    db.run(UPSERT_SQL, [
      progress.id,
      tenant,
      progress.userId,
      progress.lessonId,
      progress.completedAt,
      progress.score,
      progress.xpAwarded,
      progress.updatedAt,
    ]),
  );
}

export async function getLessonProgress(
  languageId: EntityId,
  userId: EntityId,
  lessonId: EntityId,
  store?: LocalStore,
): Promise<LearnerProgress | null> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const row = await db.getFirst<LearnerProgressRow>(
      `SELECT * FROM learner_progress WHERE language_id = ? AND user_id = ? AND lesson_id = ?`,
      [tenant, userId, lessonId],
    );
    return row ? mapLearnerProgress(row) : null;
  });
}

export async function listLessonProgressForUser(
  languageId: EntityId,
  userId: EntityId,
  store?: LocalStore,
): Promise<LearnerProgress[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<LearnerProgressRow>(
      `SELECT * FROM learner_progress WHERE language_id = ? AND user_id = ?`,
      [tenant, userId],
    );
    return rows.map(mapLearnerProgress);
  });
}
