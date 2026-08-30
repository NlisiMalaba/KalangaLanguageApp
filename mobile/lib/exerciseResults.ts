import { MAX_LESSON_SCORE } from '@/constants/progress';
import type { EntityId } from '@/domain/entities';
import { newExerciseResultId } from '@/domain/progress/ids';
import type { ExerciseScoreSample } from '@/domain/progress/types';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId } from '@/lib/localStore';
import { boolToSql } from '@/lib/mappers';
import { enqueueLearnerWrite } from '@/lib/sync/enqueueLearnerWrite';

export type ExerciseResultRow = {
  lesson_id: string;
  score: number;
};

export async function insertExerciseResult(
  input: {
    languageId: EntityId;
    userId: EntityId;
    exerciseId: EntityId;
    lessonId: EntityId;
    isCorrect: boolean;
    score: number;
    answeredAt: string;
  },
  store?: LocalStore,
): Promise<void> {
  const tenant = requireLanguageId(input.languageId);
  const score = Math.min(MAX_LESSON_SCORE, Math.max(0, input.score));
  const id = newExerciseResultId();
  const row = { id, ...input, score };
  await withStore(store, (db) =>
    db.run(
      `INSERT INTO exercise_results (
        id, language_id, user_id, exercise_id, lesson_id, is_correct, score, answered_at
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        id,
        tenant,
        input.userId,
        input.exerciseId,
        input.lessonId,
        boolToSql(input.isCorrect),
        score,
        input.answeredAt,
      ],
    ),
  );
  await enqueueLearnerWrite(
    {
      languageId: tenant,
      userId: input.userId,
      entityType: 'exercise_result',
      clientOperationId: `exercise_result:${id}`,
      payload: row,
    },
    store,
  );
}

export async function listExerciseScoresForUser(
  languageId: EntityId,
  userId: EntityId,
  store?: LocalStore,
): Promise<ExerciseScoreSample[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<ExerciseResultRow>(
      `SELECT lesson_id, score FROM exercise_results WHERE language_id = ? AND user_id = ?`,
      [tenant, userId],
    );
    return rows.map((row) => ({ lessonId: row.lesson_id, score: row.score }));
  });
}
