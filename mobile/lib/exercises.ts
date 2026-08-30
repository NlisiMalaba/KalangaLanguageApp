import type { EntityId, Exercise } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';
import { mapExercise, type ExerciseRow } from '@/lib/mappers';

const UPSERT_SQL = `
INSERT INTO exercises (
  id, language_id, lesson_id, exercise_type, prompt_data, correct_answer, sort_order, created_at
) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
ON CONFLICT(id) DO UPDATE SET
  exercise_type = excluded.exercise_type,
  prompt_data = excluded.prompt_data,
  correct_answer = excluded.correct_answer,
  sort_order = excluded.sort_order
WHERE exercises.language_id = excluded.language_id
`;

export async function upsertExercise(
  languageId: EntityId,
  exercise: Exercise,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, exercise.languageId);
  await withStore(store, (db) =>
    db.run(UPSERT_SQL, [
      exercise.id,
      tenant,
      exercise.lessonId,
      exercise.exerciseType,
      exercise.promptData,
      exercise.correctAnswer,
      exercise.sortOrder,
      exercise.createdAt,
    ]),
  );
}

export async function listExercisesForLesson(
  languageId: EntityId,
  lessonId: EntityId,
  store?: LocalStore,
): Promise<Exercise[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<ExerciseRow>(
      `SELECT * FROM exercises WHERE language_id = ? AND lesson_id = ? ORDER BY sort_order`,
      [tenant, lessonId],
    );
    return rows.map(mapExercise);
  });
}
