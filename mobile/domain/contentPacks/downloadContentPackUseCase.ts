import { MIN_FREE_DISK_BYTES } from '@/constants/contentPacks';
import { localAudioFileUri } from '@/domain/audio/localAudioPath';
import { applyRangeChunk, downloadPercent, uniqueManifestAudio } from '@/domain/contentPacks/bytes';
import { ContentPackApiError } from '@/domain/contentPacks/errors';
import type {
  ContentPackManifest,
  DownloadContentPackInput,
  DownloadContentPackResult,
  PackDownloadFileState,
  PackDownloadProgress,
} from '@/domain/contentPacks/types';
import type { ContentPack, EntityId } from '@/domain/entities';

export type RangeFetchResult = {
  status: number;
  body: Uint8Array;
};

export type ContentPackDownloaderDeps = {
  getManifest: (languageId: EntityId, packId: EntityId) => Promise<ContentPackManifest>;
  getFreeDiskBytes: () => Promise<number>;
  documentDirectory: () => string;
  ensureDirectory: (directoryUri: string) => Promise<void>;
  fileSize: (fileUri: string) => Promise<number>;
  readFile: (fileUri: string) => Promise<Uint8Array>;
  writeFile: (fileUri: string, bytes: Uint8Array) => Promise<void>;
  fetchRange: (url: string, startByte: number) => Promise<RangeFetchResult>;
  getProgress: (
    languageId: EntityId,
    packId: EntityId,
    recordingId: EntityId,
  ) => Promise<PackDownloadFileState | null>;
  upsertProgress: (languageId: EntityId, progress: PackDownloadFileState) => Promise<void>;
  listProgress: (languageId: EntityId, packId: EntityId) => Promise<PackDownloadFileState[]>;
  upsertPack: (languageId: EntityId, pack: ContentPack) => Promise<void>;
  now?: () => Date;
  newProgressId?: () => string;
  onProgress?: (progress: PackDownloadProgress) => void;
};

function newId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `dl-${Date.now()}`;
}

function directoryUri(documentDirectory: string, languageId: EntityId): string {
  const base = documentDirectory.endsWith('/') ? documentDirectory : `${documentDirectory}/`;
  return `${base}offline/audio/${languageId}`;
}

function sliceBody(body: Uint8Array, startByte: number, status: number): Uint8Array {
  if (startByte > 0 && status === 200 && body.length > startByte) {
    return body.subarray(startByte);
  }

  return body;
}

function toLocalPack(manifest: ContentPackManifest, now: Date): ContentPack {
  const instant = now.toISOString();
  return {
    id: manifest.packId,
    languageId: manifest.languageId,
    name: manifest.name,
    level: manifest.level,
    category: manifest.category,
    version: manifest.version,
    sizeBytes: manifest.sizeBytes,
    manifestUrl: '',
    lessonIds: manifest.lessons.map((lesson) => lesson.lessonId),
    createdAt: manifest.generatedAt,
    updatedAt: instant,
  };
}

export function createContentPackDownloader(deps: ContentPackDownloaderDeps) {
  const now = deps.now ?? (() => new Date());
  const newProgressId = deps.newProgressId ?? newId;

  return async function downloadContentPack(
    input: DownloadContentPackInput,
  ): Promise<DownloadContentPackResult> {
    const manifest = await deps.getManifest(input.languageId, input.packId);
    if (manifest.languageId !== input.languageId) {
      throw new ContentPackApiError('Content pack does not belong to this language.');
    }

    await deps.upsertPack(input.languageId, toLocalPack(manifest, now()));
    const files = uniqueManifestAudio(manifest);
    const bytesTotal = files.reduce((sum, file) => sum + Math.max(0, file.fileSizeBytes), 0);
    const directory = directoryUri(deps.documentDirectory(), input.languageId);
    await deps.ensureDirectory(directory);

    let filesCompleted = 0;
    let pausedForStorage = false;

    const emit = async () => {
      const rows = await deps.listProgress(input.languageId, input.packId);
      const downloaded = rows.reduce((sum, row) => sum + row.bytesDownloaded, 0);
      const snapshot = {
        percent: downloadPercent(downloaded, bytesTotal),
        bytesDownloaded: downloaded,
        bytesTotal,
        pausedForStorage,
      };
      deps.onProgress?.(snapshot);
      input.onProgress?.(snapshot);
    };

    for (const file of files) {
      const free = await deps.getFreeDiskBytes();
      if (free < MIN_FREE_DISK_BYTES) {
        pausedForStorage = true;
        const existing = await deps.getProgress(input.languageId, input.packId, file.audioRecordingId);
        const instant = now().toISOString();
        await deps.upsertProgress(input.languageId, {
          id: existing?.id ?? newProgressId(),
          languageId: input.languageId,
          contentPackId: input.packId,
          recordingId: file.audioRecordingId,
          bytesDownloaded: existing?.bytesDownloaded ?? 0,
          bytesTotal: file.fileSizeBytes,
          localPath: existing?.localPath ?? null,
          status: 'paused',
          updatedAt: instant,
        });
        await emit();
        break;
      }

      const dest = localAudioFileUri(
        deps.documentDirectory(),
        input.languageId,
        file.audioRecordingId,
        file.fileFormat,
      );
      const prior = await deps.getProgress(input.languageId, input.packId, file.audioRecordingId);
      const onDisk = await deps.fileSize(dest);
      const startByte = Math.max(prior?.bytesDownloaded ?? 0, onDisk);

      if (startByte >= file.fileSizeBytes && file.fileSizeBytes > 0) {
        filesCompleted += 1;
        await deps.upsertProgress(input.languageId, {
          id: prior?.id ?? newProgressId(),
          languageId: input.languageId,
          contentPackId: input.packId,
          recordingId: file.audioRecordingId,
          bytesDownloaded: file.fileSizeBytes,
          bytesTotal: file.fileSizeBytes,
          localPath: dest,
          status: 'complete',
          updatedAt: now().toISOString(),
        });
        await emit();
        continue;
      }

      const inProgress: PackDownloadFileState = {
        id: prior?.id ?? newProgressId(),
        languageId: input.languageId,
        contentPackId: input.packId,
        recordingId: file.audioRecordingId,
        bytesDownloaded: startByte,
        bytesTotal: file.fileSizeBytes,
        localPath: dest,
        status: 'in_progress',
        updatedAt: now().toISOString(),
      };
      await deps.upsertProgress(input.languageId, inProgress);

      const range = await deps.fetchRange(file.cdnUrl, startByte);
      const existingBytes = startByte > 0 ? await deps.readFile(dest) : new Uint8Array();
      const chunk = sliceBody(range.body, startByte, range.status);
      const merged = applyRangeChunk(existingBytes, chunk, Math.min(startByte, existingBytes.length));
      await deps.writeFile(dest, merged);

      await deps.upsertProgress(input.languageId, {
        ...inProgress,
        bytesDownloaded: merged.length,
        localPath: dest,
        status:
          merged.length >= file.fileSizeBytes
            ? 'complete'
            : 'in_progress',
        updatedAt: now().toISOString(),
      });
      if (merged.length >= file.fileSizeBytes) {
        filesCompleted += 1;
      }

      await emit();
    }

    const rows = await deps.listProgress(input.languageId, input.packId);
    const downloaded = rows.reduce((sum, row) => sum + row.bytesDownloaded, 0);

    return {
      packId: input.packId,
      percent: downloadPercent(downloaded, bytesTotal),
      pausedForStorage,
      filesCompleted,
      filesTotal: files.length,
    };
  };
}
