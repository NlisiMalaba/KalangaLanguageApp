import * as fc from 'fast-check';

import { summarizeStorage, totalStorageBytes } from '@/domain/contentPacks/storage';
import { createStorageManager } from '@/domain/contentPacks/storageManager';
import type { ContentPack } from '@/domain/entities';
import { Level } from '@/domain/enums';

const packsArb = fc.array(
  fc.record({
    id: fc.integer({ min: 1, max: 20 }),
    sizeBytes: fc.integer({ min: 0, max: 5_000_000 }),
  }),
  { minLength: 0, maxLength: 8 },
);

describe('storage total equals sum of pack sizes', () => {
  // Feature: kalanga-language-app, Property 40: Storage Total Equals Sum of Pack Sizes
  it('reports the sum of each downloaded pack size_bytes', async () => {
    await fc.assert(
      fc.asyncProperty(packsArb, async (rows) => {
        const unique = new Map<number, number>();
        for (const row of rows) {
          unique.set(row.id, row.sizeBytes);
        }

        const local: ContentPack[] = [...unique.entries()].map(([id, sizeBytes]) => ({
          id: `pack-${id}`,
          languageId: 'lang-1',
          name: `pack-${id}`,
          level: Level.Beginner,
          category: null,
          version: 1,
          sizeBytes,
          manifestUrl: '',
          lessonIds: [],
          createdAt: '2026-08-22T12:00:00.000Z',
          updatedAt: '2026-08-22T12:00:00.000Z',
        }));

        const expected = local.reduce((sum, pack) => sum + pack.sizeBytes, 0);
        expect(totalStorageBytes(local)).toBe(expected);
        expect(summarizeStorage(local).totalBytes).toBe(expected);

        const manager = createStorageManager({
          listLocalPacks: async () => local,
          listRemotePacks: async () => {
            throw new Error('offline');
          },
          getLocalPack: async () => null,
          listPackFiles: async () => [],
          deleteAudioFile: async () => undefined,
          deletePackContent: async () => undefined,
        });

        const summary = await manager.getSummary('lang-1');
        expect(summary.totalBytes).toBe(expected);
        expect(summary.packs.reduce((sum, pack) => sum + pack.sizeBytes, 0)).toBe(expected);
      }),
      { numRuns: 100 },
    );
  });
});
