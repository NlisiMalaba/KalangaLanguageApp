import type { LearnerGamification, LearnerProgress, SpacedRepetitionRecord } from '@/domain/entities';
import type { PendingSyncItem } from '@/domain/sync/ports';
import type { SyncPushRequest } from '@/domain/sync/types';

function asRecord(payload: string): Record<string, unknown> {
  const parsed: unknown = JSON.parse(payload);
  if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
    throw new Error('Sync queue payload is not an object.');
  }

  return parsed as Record<string, unknown>;
}

function asString(value: unknown): string | null {
  return typeof value === 'string' ? value : null;
}

function asNumber(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

export function queueItemToPushRequest(item: PendingSyncItem): SyncPushRequest | 'local-only' {
  if (item.entityType === 'exercise_result') {
    return 'local-only';
  }

  const row = asRecord(item.payload);

  if (item.entityType === 'progress') {
    const progress = row as unknown as LearnerProgress;
    return {
      clientOperationId: item.clientOperationId,
      progress: [
        {
          lessonId: progress.lessonId,
          completedAt: progress.completedAt,
          score: progress.score,
          xpAwarded: progress.xpAwarded,
          updatedAt: progress.updatedAt,
        },
      ],
      spacedRepetition: [],
      gamification: null,
    };
  }

  if (item.entityType === 'spaced_repetition') {
    const record = row as unknown as SpacedRepetitionRecord;
    return {
      clientOperationId: item.clientOperationId,
      progress: [],
      spacedRepetition: [
        {
          phraseId: record.phraseId,
          variationId: record.variationId,
          easeFactor: record.easeFactor,
          intervalDays: record.intervalDays,
          repetitions: record.repetitions,
          nextReviewAt: record.nextReviewAt,
          lastReviewedAt: record.lastReviewedAt,
          updatedAt: record.updatedAt,
        },
      ],
      gamification: null,
    };
  }

  const gamification = row as unknown as LearnerGamification;
  return {
    clientOperationId: item.clientOperationId,
    progress: [],
    spacedRepetition: [],
    gamification: {
      totalXp: asNumber(gamification.totalXp) ?? 0,
      currentStreak: gamification.currentStreak,
      longestStreak: gamification.longestStreak,
      lastActivityDate: asString(gamification.lastActivityDate),
      progressLevel: gamification.progressLevel,
      xpDelta: 0,
      updatedAt: gamification.updatedAt,
    },
  };
}
