import * as fc from 'fast-check';

import { exclusiveLessonIds } from '@/domain/contentPacks/storage';
import { createStorageManager } from '@/domain/contentPacks/storageManager';
import type { ContentPack } from '@/domain/entities';
import { Level } from '@/domain/enums';

function pack(id: string, lessonIds: string[], sizeBytes: number): ContentPack {
  return {
    id,
    languageId: 'lang-1',
    name: id,
    level: Level.Beginner,
    category: 'Everyday',
    version: 1,
    sizeBytes,
    manifestUrl: '',
    lessonIds,
    createdAt: '2026-08-22T12:00:00.000Z',
    updatedAt: '2026-08-22T12:00:00.000Z',
  };
}

const lessonArb = fc.constantFrom('l1', 'l2', 'l3', 'l4');
const packArb = fc.record({
  id: fc.constantFrom('pack-a', 'pack-b', 'pack-c'),
  sizeBytes: fc.integer({ min: 0, max: 50_000 }),
  lessonIds: fc.uniqueArray(lessonArb, { minLength: 1, maxLength: 3 }),
  fileCount: fc.integer({ min: 0, max: 3 }),
});

const packsArb = fc.uniqueArray(packArb, { selector: (item) => item.id, minLength: 1, maxLength: 3 });

describe('content pack deletion removes all associated data', () => {
  // Feature: kalanga-language-app, Property 24: Content Pack Deletion Removes All Associated Data
  it('removes the pack, its audio files, and lessons not shared with another pack', async () => {
    await fc.assert(
      fc.asyncProperty(packsArb, async (samples) => {
        const packs = samples.map((sample) => pack(sample.id, sample.lessonIds, sample.sizeBytes));
        const filesByPack = new Map(
          samples.map((sample) => [
            sample.id,
            Array.from({ length: sample.fileCount }, (_, index) => ({
              recordingId: `${sample.id}-rec-${index}`,
              localPath: index % 2 === 0 ? `file:///docs/${sample.id}-${index}.mp3` : null,
            })),
          ]),
        );
        const disk = new Set(
          [...filesByPack.values()].flatMap((files) =>
            files.map((file) => file.localPath).filter((path): path is string => path != null),
          ),
        );
        let remaining = [...packs];
        const deletedLessonBatches: string[][] = [];
        const target = packs[0];

        const manager = createStorageManager({
          listLocalPacks: async () => remaining,
          listRemotePacks: async () => [],
          getLocalPack: async (_languageId, packId) => remaining.find((item) => item.id === packId) ?? null,
          listPackFiles: async (_languageId, packId) => filesByPack.get(packId) ?? [],
          deleteAudioFile: async (path) => {
            disk.delete(path);
          },
          deletePackContent: async (input) => {
            deletedLessonBatches.push([...input.exclusiveLessonIds]);
            remaining = remaining.filter((item) => item.id !== input.packId);
            filesByPack.delete(input.packId);
          },
        });

        const expectedLessons = exclusiveLessonIds(target, packs);
        await manager.deletePack('lang-1', target.id);

        expect(remaining.some((item) => item.id === target.id)).toBe(false);
        expect(filesByPack.has(target.id)).toBe(false);
        expect(deletedLessonBatches).toEqual([expectedLessons]);
        for (const lessonId of expectedLessons) {
          expect(remaining.some((item) => item.lessonIds.includes(lessonId))).toBe(false);
        }

        for (const other of packs.filter((item) => item.id !== target.id)) {
          expect(remaining.some((item) => item.id === other.id)).toBe(true);
          for (const file of filesByPack.get(other.id) ?? []) {
            if (file.localPath) {
              expect(disk.has(file.localPath)).toBe(true);
            }
          }
        }

        for (const file of samples[0].fileCount
          ? Array.from({ length: samples[0].fileCount }, (_, index) =>
              index % 2 === 0 ? `file:///docs/${target.id}-${index}.mp3` : null,
            )
          : []) {
          if (file) {
            expect(disk.has(file)).toBe(false);
          }
        }
      }),
      { numRuns: 100 },
    );
  });
});
