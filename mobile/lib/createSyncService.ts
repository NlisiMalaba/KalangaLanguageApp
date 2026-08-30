import { createExpoNetworkStatus } from '@/lib/auth/expoNetworkStatus';
import { createSyncService } from '@/domain/sync/createSyncService';
import type { SyncService } from '@/domain/sync/createSyncService';
import { applyServerSyncChanges } from '@/lib/sync/applyServerChanges';
import { createHttpSyncApi } from '@/lib/sync/httpSyncApi';
import { getSyncCheckpoint, saveSyncCheckpoint } from '@/lib/syncCheckpoints';
import { countDeadLetterSync, listPendingSync, markSyncAttempt } from '@/lib/syncQueue';
import type { LocalStore } from '@/lib/localStore';

export function createDefaultSyncService(store?: LocalStore): SyncService {
  const network = createExpoNetworkStatus();
  const api = createHttpSyncApi();

  return createSyncService({
    isOnline: () => network.isOnline(),
    listPending: (languageId, userId, asOf) => listPendingSync(languageId, userId, asOf, store),
    markAttempt: (input) =>
      markSyncAttempt(
        input.languageId,
        input.id,
        {
          succeeded: input.succeeded,
          utcNow: input.utcNow,
          error: input.error,
          nextAttemptAt: input.nextAttemptAt,
          forceDeadLetter: input.forceDeadLetter,
        },
        store,
      ),
    push: (request) => api.push(request),
    pull: (sinceVersion) => api.pull(sinceVersion),
    applyChanges: (languageId, userId, changes) => applyServerSyncChanges(languageId, userId, changes, store),
    getCheckpoint: (languageId, userId) => getSyncCheckpoint(languageId, userId, store),
    saveCheckpoint: (checkpoint) => saveSyncCheckpoint(checkpoint, store),
    countDeadLetters: (languageId, userId) => countDeadLetterSync(languageId, userId, store),
  });
}
