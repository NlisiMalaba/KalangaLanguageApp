import { AuthApiError } from '@/domain/auth/errors';
import { MAX_SYNC_ATTEMPTS } from '@/constants/sync';
import { createSyncService } from '@/domain/sync/createSyncService';
import type { PendingSyncItem, SyncCheckpoint, SyncServiceDeps } from '@/domain/sync/ports';
import type { SyncChanges, SyncPushRequest, SyncPushResult } from '@/domain/sync/types';
import { Level } from '@/domain/enums';

export const SYNC_HARNESS_LANGUAGE_ID = 'lang-1';
export const SYNC_HARNESS_USER_ID = 'user-1';

const emptyChanges: SyncChanges = {
  progress: [],
  spacedRepetition: [],
  gamification: null,
};

export type HarnessQueueItem = PendingSyncItem & {
  nextAttemptAt: string | null;
  status: 'pending' | 'dead_letter';
};

export function emptySyncPushResult(overrides: Partial<SyncPushResult> = {}): SyncPushResult {
  return {
    syncVersion: 1,
    lastSyncedAt: '2026-08-30T12:00:00.000Z',
    idempotentReplay: false,
    serverChanges: emptyChanges,
    ...overrides,
  };
}

export function progressQueuePayload(lessonId: string, updatedAt: string): string {
  return JSON.stringify({
    id: `progress-${lessonId}`,
    languageId: SYNC_HARNESS_LANGUAGE_ID,
    userId: SYNC_HARNESS_USER_ID,
    lessonId,
    completedAt: updatedAt,
    score: 80,
    xpAwarded: 10,
    updatedAt,
  });
}

export function srsQueuePayload(phraseId: string, updatedAt: string): string {
  return JSON.stringify({
    id: `srs-${phraseId}`,
    languageId: SYNC_HARNESS_LANGUAGE_ID,
    userId: SYNC_HARNESS_USER_ID,
    phraseId,
    variationId: null,
    easeFactor: 2.5,
    intervalDays: 1,
    repetitions: 0,
    nextReviewAt: updatedAt.slice(0, 10),
    lastReviewedAt: null,
    updatedAt,
  });
}

export function gamificationQueuePayload(updatedAt: string): string {
  return JSON.stringify({
    id: 'xp-1',
    languageId: SYNC_HARNESS_LANGUAGE_ID,
    userId: SYNC_HARNESS_USER_ID,
    totalXp: 10,
    currentStreak: 1,
    longestStreak: 1,
    lastActivityDate: updatedAt.slice(0, 10),
    progressLevel: Level.Beginner,
    updatedAt,
  });
}

export function queueItemForType(
  index: number,
  entityType: PendingSyncItem['entityType'],
  updatedAt: string,
): HarnessQueueItem {
  const id = `q-${index}`;
  const base = {
    id,
    languageId: SYNC_HARNESS_LANGUAGE_ID,
    userId: SYNC_HARNESS_USER_ID,
    attempts: 0,
    maxAttempts: MAX_SYNC_ATTEMPTS,
    nextAttemptAt: null as string | null,
    status: 'pending' as const,
  };

  if (entityType === 'progress') {
    return {
      ...base,
      clientOperationId: `progress:${id}`,
      entityType,
      payload: progressQueuePayload(`lesson-${index}`, updatedAt),
    };
  }

  if (entityType === 'spaced_repetition') {
    return {
      ...base,
      clientOperationId: `srs:${id}`,
      entityType,
      payload: srsQueuePayload(`phrase-${index}`, updatedAt),
    };
  }

  if (entityType === 'gamification') {
    return {
      ...base,
      clientOperationId: `gamification:${id}`,
      entityType,
      payload: gamificationQueuePayload(updatedAt),
    };
  }

  return {
    ...base,
    clientOperationId: `exercise_result:${id}`,
    entityType: 'exercise_result',
    payload: JSON.stringify({ id: `ex-${index}` }),
  };
}

export function createSyncQueueHarness(options: {
  pending?: HarnessQueueItem[];
  failPush?: boolean;
  random?: () => number;
} = {}) {
  const pending: HarnessQueueItem[] = (options.pending ?? []).map((item) => ({ ...item }));
  const pushes: SyncPushRequest[] = [];
  const marks: {
    id: string;
    succeeded: boolean;
    forceDeadLetter?: boolean;
    nextAttemptAt?: string | null;
    delayMs: number | null;
  }[] = [];
  const checkpoints: SyncCheckpoint[] = [];
  let online = false;
  let now = new Date('2026-08-30T12:00:00.000Z');

  const deps: SyncServiceDeps = {
    isOnline: async () => online,
    listPending: async (_languageId, _userId, asOf) =>
      pending.filter(
        (item) =>
          item.status === 'pending' && (item.nextAttemptAt == null || item.nextAttemptAt <= asOf),
      ),
    markAttempt: async (input) => {
      const delayMs =
        input.nextAttemptAt != null
          ? new Date(input.nextAttemptAt).getTime() - new Date(input.utcNow).getTime()
          : null;
      marks.push({
        id: input.id,
        succeeded: input.succeeded,
        forceDeadLetter: input.forceDeadLetter,
        nextAttemptAt: input.nextAttemptAt,
        delayMs,
      });
      const row = pending.find((item) => item.id === input.id);
      if (!row) {
        return;
      }

      if (input.succeeded) {
        const index = pending.indexOf(row);
        pending.splice(index, 1);
        return;
      }

      row.attempts += 1;
      if (input.forceDeadLetter) {
        row.status = 'dead_letter';
        row.nextAttemptAt = null;
        return;
      }

      row.nextAttemptAt = input.nextAttemptAt ?? input.utcNow;
    },
    push: async (request) => {
      pushes.push(request);
      if (options.failPush) {
        throw new AuthApiError(500, 'unavailable');
      }

      return emptySyncPushResult({
        syncVersion: pushes.length,
        lastSyncedAt: now.toISOString(),
        serverChanges: {
          ...emptyChanges,
          gamification: {
            totalXp: 10,
            currentStreak: 1,
            longestStreak: 1,
            lastActivityDate: now.toISOString().slice(0, 10),
            progressLevel: Level.Beginner,
            xpDelta: 0,
            updatedAt: now.toISOString(),
          },
        },
      });
    },
    pull: async () =>
      emptySyncPushResult({
        syncVersion: checkpoints[checkpoints.length - 1]?.syncVersion ?? 0,
        lastSyncedAt: now.toISOString(),
      }),
    applyChanges: async () => undefined,
    getCheckpoint: async () => checkpoints[checkpoints.length - 1] ?? null,
    saveCheckpoint: async (checkpoint) => {
      checkpoints.push(checkpoint);
    },
    countDeadLetters: async () => pending.filter((item) => item.status === 'dead_letter').length,
    now: () => now,
    random: options.random ?? (() => 0.999999),
  };

  return {
    pending,
    pushes,
    marks,
    get online() {
      return online;
    },
    setOnline(value: boolean) {
      online = value;
    },
    setNow(value: Date) {
      now = value;
    },
    getNow: () => now,
    service: createSyncService(deps),
  };
}
