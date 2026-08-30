import { MAX_SYNC_ATTEMPTS, SYNC_BACKOFF_MS } from '@/constants/sync';

export function syncBackoffDelayMs(attemptNumber: number): number {
  const index = Math.min(Math.max(attemptNumber, 1), MAX_SYNC_ATTEMPTS) - 1;
  return SYNC_BACKOFF_MS[index] ?? SYNC_BACKOFF_MS[SYNC_BACKOFF_MS.length - 1];
}

/** Full jitter: delay in [0, cap] for the attempt. */
export function nextRetryAt(
  attemptNumber: number,
  now: Date,
  random: () => number = Math.random,
): Date {
  const cap = syncBackoffDelayMs(attemptNumber);
  const jitter = Math.floor(random() * (cap + 1));
  return new Date(now.getTime() + jitter);
}

export function isRetryableSyncStatus(status: number): boolean {
  return status === 408 || status === 429 || status >= 500;
}

export function isNonRetryableSyncStatus(status: number): boolean {
  return status === 400 || status === 401 || status === 403 || status === 409 || status === 422;
}
