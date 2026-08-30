import * as fc from 'fast-check';

import {
  createSyncQueueHarness,
  queueItemForType,
  SYNC_HARNESS_LANGUAGE_ID,
  SYNC_HARNESS_USER_ID,
} from '@/domain/sync/syncTestHarness';
import type { PendingSyncItem } from '@/domain/sync/ports';

const entityTypeArb: fc.Arbitrary<PendingSyncItem['entityType']> = fc.constantFrom(
  'progress',
  'spaced_repetition',
  'gamification',
  'exercise_result',
);

const queueArb = fc.array(entityTypeArb, { minLength: 1, maxLength: 8 });

describe('sync queue drains on connectivity', () => {
  // Feature: kalanga-language-app, Property 25: Sync Queue Drains on Connectivity
  it('keeps queued writes offline and drains them once the device is online', async () => {
    await fc.assert(
      fc.asyncProperty(queueArb, async (types) => {
        const updatedAt = '2026-08-30T12:00:00.000Z';
        const pending = types.map((entityType, index) => queueItemForType(index, entityType, updatedAt));
        const harness = createSyncQueueHarness({ pending });

        const offline = await harness.service.sync(SYNC_HARNESS_LANGUAGE_ID, SYNC_HARNESS_USER_ID);
        expect(offline.skippedOffline).toBe(true);
        expect(offline.pushed).toBe(0);
        expect(harness.pushes).toHaveLength(0);
        expect(harness.pending).toHaveLength(types.length);

        harness.setOnline(true);
        const online = await harness.service.sync(SYNC_HARNESS_LANGUAGE_ID, SYNC_HARNESS_USER_ID);
        expect(online.skippedOffline).toBe(false);
        expect(online.pushed).toBe(types.length);
        expect(online.failed).toBe(0);
        expect(harness.pending).toHaveLength(0);

        const posted = types.filter((type) => type !== 'exercise_result').length;
        expect(harness.pushes).toHaveLength(posted);
      }),
      { numRuns: 100 },
    );
  });
});
