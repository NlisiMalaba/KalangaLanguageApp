import { MIN_FREE_DISK_BYTES } from '@/constants/contentPacks';
import type { ContentPackDownloaderDeps } from '@/domain/contentPacks/downloadContentPackUseCase';
import type { ContentPackManifest, PackDownloadFileState } from '@/domain/contentPacks/types';
import type { ContentPack } from '@/domain/entities';
import { Level } from '@/domain/enums';

export const DOWNLOAD_NOW = new Date('2026-08-22T12:00:00.000Z');

export type PackFile = { id: string; size: number; bytes: Uint8Array };

export function packManifest(files: PackFile[]): ContentPackManifest {
  return {
    packId: 'pack-1',
    languageId: 'lang-1',
    name: 'Beginner Everyday',
    version: 1,
    level: Level.Beginner,
    category: 'Everyday',
    generatedAt: DOWNLOAD_NOW.toISOString(),
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

export function memoryContentPackDeps(options: {
  files: PackFile[];
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
    getManifest: async () => packManifest(options.files),
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
    now: () => DOWNLOAD_NOW,
    newProgressId: () => `progress-${progress.length + 1}`,
  };
}
