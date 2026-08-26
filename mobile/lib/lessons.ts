import type { EntityId, Lesson } from '@/domain/entities';
import { LessonStatus } from '@/domain/enums';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';
import { boolToSql, mapLesson, type LessonRow } from '@/lib/mappers';

const UPSERT_SQL = `
INSERT INTO lessons (
  id, language_id, title, level, category, is_scenario, scenario_context,
  status, contributor_id, reviewed_by, review_feedback, xp_reward, created_at, updated_at
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
ON CONFLICT(id) DO UPDATE SET
  title = excluded.title,
  level = excluded.level,
  category = excluded.category,
  is_scenario = excluded.is_scenario,
  scenario_context = excluded.scenario_context,
  status = excluded.status,
  reviewed_by = excluded.reviewed_by,
  review_feedback = excluded.review_feedback,
  xp_reward = excluded.xp_reward,
  updated_at = excluded.updated_at
WHERE lessons.language_id = excluded.language_id
`;

export async function upsertLesson(
  languageId: EntityId,
  lesson: Lesson,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, lesson.languageId);
  await withStore(store, (db) =>
    db.run(UPSERT_SQL, [
      lesson.id,
      tenant,
      lesson.title,
      lesson.level,
      lesson.category,
      boolToSql(lesson.isScenario),
      lesson.scenarioContext,
      lesson.status,
      lesson.contributorId,
      lesson.reviewedBy,
      lesson.reviewFeedback,
      lesson.xpReward,
      lesson.createdAt,
      lesson.updatedAt,
    ]),
  );
}

export async function getLesson(
  languageId: EntityId,
  lessonId: EntityId,
  store?: LocalStore,
): Promise<Lesson | null> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const row = await db.getFirst<LessonRow>(
      `SELECT * FROM lessons WHERE id = ? AND language_id = ?`,
      [lessonId, tenant],
    );
    return row ? mapLesson(row) : null;
  });
}

export async function listPublishedLessons(
  languageId: EntityId,
  store?: LocalStore,
): Promise<Lesson[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<LessonRow>(
      `SELECT * FROM lessons WHERE language_id = ? AND status = ? ORDER BY title`,
      [tenant, LessonStatus.Published],
    );
    return rows.map(mapLesson);
  });
}
