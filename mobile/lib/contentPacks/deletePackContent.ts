import type { EntityId } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId } from '@/lib/localStore';
import { sqlInPlaceholders } from '@/lib/sqlIn';

export async function deletePackContent(
  input: {
    languageId: EntityId;
    packId: EntityId;
    exclusiveLessonIds: readonly EntityId[];
  },
  store?: LocalStore,
): Promise<void> {
  const tenant = requireLanguageId(input.languageId);
  const lessons = [...input.exclusiveLessonIds];

  await withStore(store, async (db) => {
    await db.transaction(async (tx) => {
      if (lessons.length > 0) {
        const lessonSlots = sqlInPlaceholders(lessons.length);
        const lessonParams = [tenant, ...lessons];

        await tx.run(
          `DELETE FROM exercise_results WHERE language_id = ? AND lesson_id IN (${lessonSlots})`,
          lessonParams,
        );
        await tx.run(
          `DELETE FROM exercises WHERE language_id = ? AND lesson_id IN (${lessonSlots})`,
          lessonParams,
        );

        const phrases = await tx.getAll<{ id: string }>(
          `SELECT id FROM phrases WHERE language_id = ? AND lesson_id IN (${lessonSlots})`,
          lessonParams,
        );
        const phraseIds = phrases.map((row) => row.id);
        if (phraseIds.length > 0) {
          const phraseSlots = sqlInPlaceholders(phraseIds.length);
          const phraseParams = [tenant, ...phraseIds];
          const variations = await tx.getAll<{ id: string }>(
            `SELECT id FROM language_variations WHERE language_id = ? AND phrase_id IN (${phraseSlots})`,
            phraseParams,
          );
          const variationIds = variations.map((row) => row.id);
          if (variationIds.length > 0) {
            const variationSlots = sqlInPlaceholders(variationIds.length);
            await tx.run(
              `DELETE FROM audio_recordings WHERE language_id = ? AND variation_id IN (${variationSlots})`,
              [tenant, ...variationIds],
            );
          }

          await tx.run(
            `DELETE FROM audio_recordings WHERE language_id = ? AND phrase_id IN (${phraseSlots})`,
            phraseParams,
          );
          await tx.run(
            `DELETE FROM language_variations WHERE language_id = ? AND phrase_id IN (${phraseSlots})`,
            phraseParams,
          );
          await tx.run(
            `DELETE FROM spaced_repetition_records WHERE language_id = ? AND phrase_id IN (${phraseSlots})`,
            phraseParams,
          );
          await tx.run(
            `DELETE FROM phrases WHERE language_id = ? AND id IN (${phraseSlots})`,
            phraseParams,
          );
        }

        await tx.run(`DELETE FROM lessons WHERE language_id = ? AND id IN (${lessonSlots})`, lessonParams);
      }

      await tx.run(`DELETE FROM download_progress WHERE language_id = ? AND content_pack_id = ?`, [
        tenant,
        input.packId,
      ]);
      await tx.run(`DELETE FROM content_packs WHERE language_id = ? AND id = ?`, [tenant, input.packId]);
    });
  });
}
