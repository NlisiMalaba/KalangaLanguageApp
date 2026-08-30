import type { EntityId } from '@/domain/entities';
import { Level } from '@/domain/enums';
import { newGamificationId, newProgressId, newSrsId } from '@/domain/progress/ids';
import { incomingWinsLastWrite } from '@/domain/sync/lastWriteWins';
import type { SyncChanges } from '@/domain/sync/types';
import { getGamification, upsertGamification } from '@/lib/gamification';
import { getLessonProgress, upsertLessonProgress } from '@/lib/lessonProgress';
import type { LocalStore } from '@/lib/localStore';
import { findSpacedRepetition, upsertSpacedRepetition } from '@/lib/spacedRepetition';

export async function applyServerSyncChanges(
  languageId: EntityId,
  userId: EntityId,
  changes: SyncChanges,
  store?: LocalStore,
): Promise<void> {
  for (const item of changes.progress) {
    const existing = await getLessonProgress(languageId, userId, item.lessonId, store);
    if (!incomingWinsLastWrite(existing?.updatedAt, item.updatedAt)) {
      continue;
    }
    await upsertLessonProgress(
      languageId,
      {
        id: existing?.id ?? newProgressId(),
        languageId,
        userId,
        lessonId: item.lessonId,
        completedAt: item.completedAt,
        score: item.score,
        xpAwarded: item.xpAwarded,
        updatedAt: item.updatedAt,
      },
      store,
    );
  }

  for (const item of changes.spacedRepetition) {
    const existing = await findSpacedRepetition(languageId, userId, item.phraseId, item.variationId, store);
    if (!incomingWinsLastWrite(existing?.updatedAt, item.updatedAt)) {
      continue;
    }
    await upsertSpacedRepetition(
      languageId,
      {
        id: existing?.id ?? newSrsId(),
        languageId,
        userId,
        phraseId: item.phraseId,
        variationId: item.variationId,
        easeFactor: item.easeFactor,
        intervalDays: item.intervalDays,
        repetitions: item.repetitions,
        nextReviewAt: item.nextReviewAt,
        lastReviewedAt: item.lastReviewedAt,
        updatedAt: item.updatedAt,
      },
      store,
      { skipSyncQueue: true },
    );
  }

  if (changes.gamification) {
    const existing = await getGamification(languageId, userId, store);
    if (incomingWinsLastWrite(existing?.updatedAt, changes.gamification.updatedAt)) {
      await upsertGamification(
        languageId,
        {
          id: existing?.id ?? newGamificationId(),
          languageId,
          userId,
          totalXp: changes.gamification.totalXp,
          currentStreak: changes.gamification.currentStreak,
          longestStreak: changes.gamification.longestStreak,
          lastActivityDate: changes.gamification.lastActivityDate,
          progressLevel: changes.gamification.progressLevel ?? existing?.progressLevel ?? Level.Beginner,
          updatedAt: changes.gamification.updatedAt,
        },
        store,
      );
    }
  }
}
