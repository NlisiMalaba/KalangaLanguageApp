import { SYNC_BACKOFF_MS } from '@/constants/sync';
import { nextRetryAt, syncBackoffDelayMs } from '@/domain/sync/backoff';

describe('sync backoff', () => {
  it('uses 1s, 2s, 4s, 8s, 16s caps', () => {
    expect([1, 2, 3, 4, 5].map(syncBackoffDelayMs)).toEqual([...SYNC_BACKOFF_MS]);
  });

  it('applies full jitter in [0, cap]', () => {
    const now = new Date('2026-08-30T12:00:00.000Z');
    expect(nextRetryAt(1, now, () => 0).getTime()).toBe(now.getTime());
    expect(nextRetryAt(1, now, () => 0.999).getTime()).toBe(now.getTime() + 999);
    expect(nextRetryAt(5, now, () => 0.5).getTime()).toBe(now.getTime() + 8000);
  });
});
