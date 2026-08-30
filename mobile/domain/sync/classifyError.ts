import { AuthApiError, SessionExpiredError } from '@/domain/auth/errors';
import { isNonRetryableSyncStatus, isRetryableSyncStatus } from '@/domain/sync/backoff';

export type SyncFailureKind = 'retryable' | 'fatal';

export function classifySyncError(error: unknown): SyncFailureKind {
  if (error instanceof SessionExpiredError) {
    return 'fatal';
  }

  if (error instanceof AuthApiError) {
    if (isNonRetryableSyncStatus(error.status)) {
      return 'fatal';
    }

    if (isRetryableSyncStatus(error.status)) {
      return 'retryable';
    }

    if (error.status >= 400 && error.status < 500) {
      return 'fatal';
    }

    return 'retryable';
  }

  return 'retryable';
}

export function syncErrorMessage(error: unknown): string {
  if (error instanceof Error && error.message.length > 0) {
    return error.message;
  }

  return 'Sync failed.';
}
