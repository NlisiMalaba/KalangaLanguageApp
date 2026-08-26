import type { EntityId, LearnerGamification } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';
import { mapLearnerGamification, type LearnerGamificationRow } from '@/lib/mappers';

const UPSERT_SQL = `
INSERT INTO learner_gamification (
  id, language_id, user_id, total_xp, current_streak, longest_streak,
  last_activity_date, progress_level, updated_at
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
ON CONFLICT(user_id, language_id) DO UPDATE SET
  total_xp = excluded.total_xp,
  current_streak = excluded.current_streak,
  longest_streak = excluded.longest_streak,
  last_activity_date = excluded.last_activity_date,
  progress_level = excluded.progress_level,
  updated_at = excluded.updated_at
WHERE excluded.updated_at >= learner_gamification.updated_at
`;

export async function upsertGamification(
  languageId: EntityId,
  gamification: LearnerGamification,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, gamification.languageId);
  await withStore(store, (db) =>
    db.run(UPSERT_SQL, [
      gamification.id,
      tenant,
      gamification.userId,
      gamification.totalXp,
      gamification.currentStreak,
      gamification.longestStreak,
      gamification.lastActivityDate,
      gamification.progressLevel,
      gamification.updatedAt,
    ]),
  );
}

export async function getGamification(
  languageId: EntityId,
  userId: EntityId,
  store?: LocalStore,
): Promise<LearnerGamification | null> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const row = await db.getFirst<LearnerGamificationRow>(
      `SELECT * FROM learner_gamification WHERE language_id = ? AND user_id = ?`,
      [tenant, userId],
    );
    return row ? mapLearnerGamification(row) : null;
  });
}
