import { nextRetryAt } from '@/domain/sync/backoff';
import { classifySyncError, syncErrorMessage } from '@/domain/sync/classifyError';
import { DuplicateSyncPushError } from '@/domain/sync/errors';
import type { EntityId } from '@/domain/entities';
import type { SyncServiceDeps } from '@/domain/sync/ports';
import { queueItemToPushRequest } from '@/domain/sync/queuePayload';
import type { SyncPushResult, SyncRunResult, SyncStatus } from '@/domain/sync/types';

export function createSyncService(deps: SyncServiceDeps) {
  const now = () => (deps.now ? deps.now() : new Date());
  const random = () => (deps.random ? deps.random() : Math.random());
  let inflight: Promise<SyncRunResult> | null = null;

  async function persistResult(languageId: EntityId, userId: EntityId, result: SyncPushResult): Promise<void> {
    await deps.applyChanges(languageId, userId, result.serverChanges);
    await deps.saveCheckpoint({
      languageId,
      userId,
      syncVersion: result.syncVersion,
      lastSyncedAt: result.lastSyncedAt,
    });
  }

  async function syncOnce(languageId: EntityId, userId: EntityId): Promise<SyncRunResult> {
    const result: SyncRunResult = { pushed: 0, failed: 0, deadLettered: 0, skippedOffline: false };
    if (!(await deps.isOnline())) {
      result.skippedOffline = true;
      return result;
    }

    const instant = now();
    const asOf = instant.toISOString();
    const pending = await deps.listPending(languageId, userId, asOf);

    for (const item of pending) {
      const request = queueItemToPushRequest(item);
      try {
        if (request === 'local-only') {
          await deps.markAttempt({
            languageId,
            id: item.id,
            succeeded: true,
            utcNow: now().toISOString(),
          });
          result.pushed += 1;
          continue;
        }

        let pushResult: SyncPushResult;
        try {
          pushResult = await deps.push(request);
        } catch (error) {
          if (error instanceof DuplicateSyncPushError) {
            const checkpoint = await deps.getCheckpoint(languageId, userId);
            pushResult = await deps.pull(checkpoint?.syncVersion ?? 0);
          } else {
            throw error;
          }
        }

        await persistResult(languageId, userId, pushResult);
        await deps.markAttempt({
          languageId,
          id: item.id,
          succeeded: true,
          utcNow: now().toISOString(),
        });
        result.pushed += 1;
      } catch (error) {
        const kind = classifySyncError(error);
        const message = syncErrorMessage(error);
        const attemptsAfter = item.attempts + 1;
        const forceDead = kind === 'fatal' || attemptsAfter >= item.maxAttempts;
        result.failed += 1;
        if (forceDead) {
          result.deadLettered += 1;
        }

        await deps.markAttempt({
          languageId,
          id: item.id,
          succeeded: false,
          utcNow: now().toISOString(),
          error: message,
          forceDeadLetter: forceDead,
          nextAttemptAt: nextRetryAt(attemptsAfter, now(), random).toISOString(),
        });
      }
    }

    const checkpoint = await deps.getCheckpoint(languageId, userId);
    const pulled = await deps.pull(checkpoint?.syncVersion ?? 0);
    await persistResult(languageId, userId, pulled);
    return result;
  }

  return {
    sync(languageId: EntityId, userId: EntityId): Promise<SyncRunResult> {
      if (inflight) {
        return inflight;
      }

      inflight = syncOnce(languageId, userId).finally(() => {
        inflight = null;
      });
      return inflight;
    },
    async status(languageId: EntityId, userId: EntityId): Promise<SyncStatus> {
      const checkpoint = await deps.getCheckpoint(languageId, userId);
      return {
        lastSyncedAt: checkpoint?.lastSyncedAt ?? null,
        syncVersion: checkpoint?.syncVersion ?? 0,
        deadLetterCount: await deps.countDeadLetters(languageId, userId),
      };
    },
  };
}

export type SyncService = ReturnType<typeof createSyncService>;
