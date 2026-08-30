import { AuthApiError } from '@/domain/auth/errors';
import { createSyncService } from '@/domain/sync/createSyncService';
import type { PendingSyncItem, SyncCheckpoint, SyncServiceDeps } from '@/domain/sync/ports';
import type { SyncChanges, SyncPushRequest, SyncPushResult } from '@/domain/sync/types';
import { Level } from '@/domain/enums';

const now = new Date('2026-08-30T12:00:00.000Z');
const languageId = 'lang-1';
const userId = 'user-1';

const emptyChanges: SyncChanges = {
  progress: [],
  spacedRepetition: [],
  gamification: null,
};

function pushResult(overrides: Partial<SyncPushResult> = {}): SyncPushResult {
  return {
    syncVersion: 4,
    lastSyncedAt: now.toISOString(),
    idempotentReplay: false,
    serverChanges: {
      ...emptyChanges,
      gamification: {
        totalXp: 10,
        currentStreak: 1,
        longestStreak: 1,
        lastActivityDate: '2026-08-30',
        progressLevel: Level.Beginner,
        xpDelta: 0,
        updatedAt: now.toISOString(),
      },
    },
    ...overrides,
  };
}

function progressItem(overrides: Partial<PendingSyncItem> = {}): PendingSyncItem {
  return {
    id: 'q-1',
    languageId,
    userId,
    clientOperationId: 'progress:user-1:lesson-1:t',
    entityType: 'progress',
    payload: JSON.stringify({
      id: 'p-1',
      languageId,
      userId,
      lessonId: 'lesson-1',
      completedAt: now.toISOString(),
      score: 90,
      xpAwarded: 10,
      updatedAt: now.toISOString(),
    }),
    attempts: 0,
    maxAttempts: 5,
    ...overrides,
  };
}

function memoryDeps(options: {
  online?: boolean;
  pending?: PendingSyncItem[];
  push?: (request: SyncPushRequest) => Promise<SyncPushResult>;
  pull?: (since: number) => Promise<SyncPushResult>;
}): SyncServiceDeps & {
  marks: { id: string; succeeded: boolean; forceDeadLetter?: boolean; nextAttemptAt?: string | null }[];
  applied: SyncChanges[];
  checkpoints: SyncCheckpoint[];
  pushes: SyncPushRequest[];
} {
  const marks: {
    id: string;
    succeeded: boolean;
    forceDeadLetter?: boolean;
    nextAttemptAt?: string | null;
  }[] = [];
  const applied: SyncChanges[] = [];
  const checkpoints: SyncCheckpoint[] = [];
  const pushes: SyncPushRequest[] = [];
  let pending = [...(options.pending ?? [])];

  return {
    marks,
    applied,
    checkpoints,
    pushes,
    isOnline: async () => options.online ?? true,
    listPending: async () => pending.filter((item) => item.attempts < 99),
    markAttempt: async (input) => {
      marks.push({
        id: input.id,
        succeeded: input.succeeded,
        forceDeadLetter: input.forceDeadLetter,
        nextAttemptAt: input.nextAttemptAt,
      });
      if (input.succeeded || input.forceDeadLetter) {
        pending = pending.filter((item) => item.id !== input.id);
      } else {
        pending = pending.map((item) =>
          item.id === input.id ? { ...item, attempts: item.attempts + 1 } : item,
        );
      }
    },
    push: async (request) => {
      pushes.push(request);
      if (options.push) {
        return options.push(request);
      }

      return pushResult();
    },
    pull: async (since) => {
      if (options.pull) {
        return options.pull(since);
      }

      return pushResult({ syncVersion: 5, serverChanges: emptyChanges });
    },
    applyChanges: async (_languageId, _userId, changes) => {
      applied.push(changes);
    },
    getCheckpoint: async () => checkpoints[checkpoints.length - 1] ?? null,
    saveCheckpoint: async (checkpoint) => {
      checkpoints.push(checkpoint);
    },
    countDeadLetters: async () => marks.filter((row) => row.forceDeadLetter).length,
    now: () => now,
    random: () => 0,
  };
}

describe('createSyncService', () => {
  it('skips when offline', async () => {
    const deps = memoryDeps({ online: false, pending: [progressItem()] });
    const result = await createSyncService(deps).sync(languageId, userId);
    expect(result.skippedOffline).toBe(true);
    expect(deps.pushes).toHaveLength(0);
  });

  it('drains the queue, applies server changes, and stores the checkpoint', async () => {
    const deps = memoryDeps({ pending: [progressItem()] });
    const result = await createSyncService(deps).sync(languageId, userId);
    expect(result.pushed).toBe(1);
    expect(deps.pushes[0]?.progress[0]?.lessonId).toBe('lesson-1');
    expect(deps.applied[0]?.gamification?.totalXp).toBe(10);
    expect(deps.checkpoints[0]?.syncVersion).toBe(4);
    expect(deps.marks[0]?.succeeded).toBe(true);
    expect(deps.checkpoints[deps.checkpoints.length - 1]?.syncVersion).toBe(5);
  });

  it('acks exercise results locally without posting them', async () => {
    const deps = memoryDeps({
      pending: [
        progressItem({
          id: 'q-ex',
          entityType: 'exercise_result',
          clientOperationId: 'exercise_result:1',
          payload: JSON.stringify({ id: 'ex-1' }),
        }),
      ],
    });
    await createSyncService(deps).sync(languageId, userId);
    expect(deps.pushes).toHaveLength(0);
    expect(deps.marks[0]).toMatchObject({ id: 'q-ex', succeeded: true });
  });

  it('schedules exponential backoff on retryable failures', async () => {
    const deps = memoryDeps({
      pending: [progressItem()],
      push: async () => {
        throw new AuthApiError(500, 'unavailable');
      },
    });
    const result = await createSyncService(deps).sync(languageId, userId);
    expect(result.failed).toBe(1);
    expect(result.deadLettered).toBe(0);
    expect(deps.marks[0]?.succeeded).toBe(false);
    expect(deps.marks[0]?.nextAttemptAt).toBe(now.toISOString());
  });

  it('dead-letters after 5 retryable failures and on non-retryable status', async () => {
    const fifth = memoryDeps({
      pending: [progressItem({ attempts: 4 })],
      push: async () => {
        throw new AuthApiError(503, 'unavailable');
      },
    });
    const afterFive = await createSyncService(fifth).sync(languageId, userId);
    expect(afterFive.deadLettered).toBe(1);
    expect(fifth.marks[0]?.forceDeadLetter).toBe(true);

    const fatal = memoryDeps({
      pending: [progressItem({ id: 'q-400' })],
      push: async () => {
        throw new AuthApiError(400, 'bad payload');
      },
    });
    const afterFatal = await createSyncService(fatal).sync(languageId, userId);
    expect(afterFatal.deadLettered).toBe(1);
    expect(fatal.marks[0]?.forceDeadLetter).toBe(true);
  });

  it('exposes last successful sync and dead-letter count', async () => {
    const deps = memoryDeps({});
    deps.checkpoints.push({
      languageId,
      userId,
      syncVersion: 9,
      lastSyncedAt: now.toISOString(),
    });
    const status = await createSyncService(deps).status(languageId, userId);
    expect(status).toEqual({
      lastSyncedAt: now.toISOString(),
      syncVersion: 9,
      deadLetterCount: 0,
    });
  });
});
