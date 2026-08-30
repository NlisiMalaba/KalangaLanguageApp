import { MIN_FREE_DISK_BYTES } from '@/constants/contentPacks';
import { createContentPackDownloader } from '@/domain/contentPacks/downloadContentPackUseCase';
import type { ContentPackDownloaderDeps } from '@/domain/contentPacks/downloadContentPackUseCase';
import type { ContentPackManifest, PackDownloadFileState } from '@/domain/contentPacks/types';
import type { ContentPack } from '@/domain/entities';
import { Level } from '@/domain/enums';

const now = new Date('2026-08-22T12:00:00.000Z');

function manifest(files: { id: string; size: number; bytes: Uint8Array }[]): ContentPackManifest {
  return {
    packId: 'pack-1',
    languageId: 'lang-1',
    name: 'Beginner Everyday',
    version: 1,
    level: Level.Beginner,
    category: 'Everyday',
    generatedAt: now.toISOString(),
    sizeBytes: files.reduce((sum, file) => sum + file.size, 0),
    lessons: [
      {
        lessonId: 'lesson-1',
        audio: files.map((file) => ({
          audioRecordingId: file.id,
          cdnUrl: `https://cdn.example/${file.id}.mp3`,
          fileFormat: 'Mp3',
          fileSizeBytes: file.size,
        })),
      },
    ],
  };
}

function memoryDeps(options: {
  files: { id: string; size: number; bytes: Uint8Array }[];
  freeBytes?: number;
  seed?: { recordingId: string; bytes: Uint8Array };
}): ContentPackDownloaderDeps & {
  filesOnDisk: Map<string, Uint8Array>;
  progress: PackDownloadFileState[];
  packs: ContentPack[];
  rangeStarts: number[];
} {
  const filesOnDisk = new Map<string, Uint8Array>();
  if (options.seed) {
    filesOnDisk.set(
      `file:///docs/offline/audio/lang-1/${options.seed.recordingId}.mp3`,
      options.seed.bytes,
    );
  }

  const progress: PackDownloadFileState[] = [];
  const packs: ContentPack[] = [];
  const rangeStarts: number[] = [];
  const bodies = new Map(options.files.map((file) => [file.id, file.bytes]));

  return {
    filesOnDisk,
    progress,
    packs,
    rangeStarts,
    getManifest: async () => manifest(options.files),
    getFreeDiskBytes: async () => options.freeBytes ?? MIN_FREE_DISK_BYTES + 1,
    documentDirectory: () => 'file:///docs/',
    ensureDirectory: async () => undefined,
    fileSize: async (uri) => filesOnDisk.get(uri)?.length ?? 0,
    readFile: async (uri) => filesOnDisk.get(uri) ?? new Uint8Array(),
    writeFile: async (uri, bytes) => {
      filesOnDisk.set(uri, bytes);
    },
    fetchRange: async (url, startByte) => {
      rangeStarts.push(startByte);
      const id = url.split('/').pop()?.replace('.mp3', '') ?? '';
      const full = bodies.get(id) ?? new Uint8Array();
      return { status: startByte > 0 ? 206 : 200, body: full.subarray(startByte) };
    },
    getProgress: async (_languageId, _packId, recordingId) =>
      progress.find((row) => row.recordingId === recordingId) ?? null,
    upsertProgress: async (_languageId, row) => {
      const index = progress.findIndex((item) => item.recordingId === row.recordingId);
      if (index >= 0) {
        progress[index] = row;
        return;
      }

      progress.push(row);
    },
    listProgress: async () => progress,
    upsertPack: async (_languageId, pack) => {
      packs[0] = pack;
    },
    now: () => now,
    newProgressId: () => 'progress-1',
  };
}

describe('downloadContentPackUseCase', () => {
  it('writes audio files, tracks progress, and sends Range after a partial file', async () => {
    const first = new Uint8Array([1, 2, 3, 4]);
    const deps = memoryDeps({
      files: [{ id: 'rec-1', size: 4, bytes: first }],
      seed: { recordingId: 'rec-1', bytes: new Uint8Array([1, 2]) },
    });
    const percents: number[] = [];
    const download = createContentPackDownloader({
      ...deps,
      onProgress: (snapshot) => percents.push(snapshot.percent),
    });

    const result = await download({ languageId: 'lang-1', packId: 'pack-1' });
    expect(result.pausedForStorage).toBe(false);
    expect(result.percent).toBe(100);
    expect(result.filesCompleted).toBe(1);
    expect(deps.rangeStarts).toEqual([2]);
    expect([...deps.filesOnDisk.values()][0]).toEqual(first);
    expect(deps.progress[0]?.status).toBe('complete');
    expect(percents.at(-1)).toBe(100);
  });

  it('pauses and reports storage warning when free space is below 50 MB', async () => {
    const deps = memoryDeps({
      files: [{ id: 'rec-1', size: 4, bytes: new Uint8Array([1, 2, 3, 4]) }],
      freeBytes: MIN_FREE_DISK_BYTES - 1,
    });
    const download = createContentPackDownloader(deps);
    const result = await download({ languageId: 'lang-1', packId: 'pack-1' });
    expect(result.pausedForStorage).toBe(true);
    expect(result.percent).toBe(0);
    expect(deps.rangeStarts).toEqual([]);
    expect(deps.progress[0]?.status).toBe('paused');
  });
});
