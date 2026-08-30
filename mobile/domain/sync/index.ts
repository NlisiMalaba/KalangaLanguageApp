export { nextRetryAt, syncBackoffDelayMs } from './backoff';
export { classifySyncError } from './classifyError';
export { createSyncService } from './createSyncService';
export type { SyncService } from './createSyncService';
export { incomingWinsLastWrite, takeLastWriteWins } from './lastWriteWins';
export type { PendingSyncItem, SyncCheckpoint, SyncServiceDeps } from './ports';
export type { SyncChanges, SyncPushRequest, SyncPushResult, SyncRunResult, SyncStatus } from './types';
