import * as fc from 'fast-check';

import { MAX_SYNC_ATTEMPTS, SYNC_BACKOFF_MS } from '@/constants/sync';
import {
  createSyncQueueHarness,
  queueItemForType,
  SYNC_HARNESS_LANGUAGE_ID,
  SYNC_HARNESS_USER_ID,
} from '@/domain/sync/syncTestHarness';

const seedArb = fc.integer({ min: 0, max: 1_000_000 });

describe('exponential backoff retry on sync failure', () => {
  // Feature: kalanga-language-app, Property 27: Exponential Backoff Retry on Sync Failure
  it('retries with 1s, 2s, 4s, 8s, 16s caps and stops after 5 attempts', async () => {
    await fc.assert(
      fc.asyncProperty(seedArb, async (seed) => {
        const harness = createSyncQueueHarness({
          pending: [queueItemForType(seed % 8, 'progress', '2026-08-30T12:00:00.000Z')],
          failPush: true,
          random: () => 0.999999,
        });
        harness.setOnline(true);

        const delays: number[] = [];
        for (let attempt = 0; attempt < MAX_SYNC_ATTEMPTS + 2; attempt += 1) {
          const result = await harness.service.sync(SYNC_HARNESS_LANGUAGE_ID, SYNC_HARNESS_USER_ID);
          const mark = harness.marks[harness.marks.length - 1];
          if (result.skippedOffline) {
            throw new Error('expected an online sync');
          }

          if (mark && mark.succeeded === false && mark.delayMs != null) {
            delays.push(mark.delayMs);
          }

          if (result.deadLettered > 0) {
            break;
          }

          const nextAt = mark?.nextAttemptAt;
          if (nextAt) {
            harness.setNow(new Date(nextAt));
          }
        }

        expect(delays).toEqual([...SYNC_BACKOFF_MS]);
        expect(harness.pushes).toHaveLength(MAX_SYNC_ATTEMPTS);
        expect(harness.pending[0]?.status).toBe('dead_letter');
        expect(harness.pending[0]?.attempts).toBe(MAX_SYNC_ATTEMPTS);

        const extra = await harness.service.sync(SYNC_HARNESS_LANGUAGE_ID, SYNC_HARNESS_USER_ID);
        expect(extra.pushed).toBe(0);
        expect(extra.failed).toBe(0);
        expect(harness.pushes).toHaveLength(MAX_SYNC_ATTEMPTS);
      }),
      { numRuns: 100 },
    );
  });
});
