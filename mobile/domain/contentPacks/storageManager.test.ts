import { ContentPackNotFoundError } from '@/domain/contentPacks/errors';
import { exclusiveLessonIds, summarizeStorage, totalStorageBytes } from '@/domain/contentPacks/storage';
import { createStorageManager } from '@/domain/contentPacks/storageManager';
import type { ContentPack } from '@/domain/entities';
import { Level } from '@/domain/enums';

function pack(id: string, sizeBytes: number, lessonIds: string[], version = 1): ContentPack {
  return {
    id,
    languageId: 'lang-1',
    name: id,
    level: Level.Beginner,
    category: 'Everyday',
    version,
    sizeBytes,
    manifestUrl: '',
    lessonIds,
    createdAt: '2026-08-22T12:00:00.000Z',
    updatedAt: '2026-08-22T12:00:00.000Z',
  };
}

describe('storageManager', () => {
  it('totals size_bytes and flags a newer remote version', async () => {
    const local = [pack('a', 100, ['l1']), pack('b', 50, ['l2'])];
    const manager = createStorageManager({
      listLocalPacks: async () => local,
      listRemotePacks: async () => [
        {
          packId: 'a',
          languageId: 'lang-1',
          name: 'a',
          level: Level.Beginner,
          category: 'Everyday',
          version: 3,
          sizeBytes: 120,
          manifestUrl: 'https://cdn.example/a',
        },
      ],
      getLocalPack: async () => null,
      listPackFiles: async () => [],
      deleteAudioFile: async () => undefined,
      deletePackContent: async () => undefined,
    });

    const summary = await manager.getSummary('lang-1');
    expect(summary.totalBytes).toBe(150);
    expect(totalStorageBytes(local)).toBe(150);
    expect(summary.packs.find((item) => item.packId === 'a')?.updateAvailable).toBe(true);
    expect(summary.packs.find((item) => item.packId === 'b')?.updateAvailable).toBe(false);
  });

  it('deletes exclusive lessons and audio files for a pack', async () => {
    const a = pack('a', 100, ['shared', 'only-a']);
    const b = pack('b', 50, ['shared']);
    const deletedFiles: string[] = [];
    let deletedLessons: string[] = [];
    const manager = createStorageManager({
      listLocalPacks: async () => [a, b],
      listRemotePacks: async () => [],
      getLocalPack: async () => a,
      listPackFiles: async () => [{ recordingId: 'rec-1', localPath: 'file:///docs/a.mp3' }],
      deleteAudioFile: async (path) => {
        deletedFiles.push(path);
      },
      deletePackContent: async (input) => {
        deletedLessons = [...input.exclusiveLessonIds];
      },
    });

    expect(exclusiveLessonIds(a, [a, b])).toEqual(['only-a']);
    await manager.deletePack('lang-1', 'a');
    expect(deletedFiles).toEqual(['file:///docs/a.mp3']);
    expect(deletedLessons).toEqual(['only-a']);
  });

  it('rejects deleting a pack that is not on the device', async () => {
    const manager = createStorageManager({
      listLocalPacks: async () => [],
      listRemotePacks: async () => [],
      getLocalPack: async () => null,
      listPackFiles: async () => [],
      deleteAudioFile: async () => undefined,
      deletePackContent: async () => undefined,
    });

    await expect(manager.deletePack('lang-1', 'missing')).rejects.toBeInstanceOf(ContentPackNotFoundError);
  });

  it('still summarizes when the remote catalog is unreachable', async () => {
    const summary = summarizeStorage([pack('a', 10, ['l1'])], []);
    expect(summary.totalBytes).toBe(10);
    expect(summary.packs[0]?.updateAvailable).toBe(false);
  });
});
