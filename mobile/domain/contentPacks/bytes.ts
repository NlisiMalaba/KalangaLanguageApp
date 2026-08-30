import { ContentPackDownloadError } from '@/domain/contentPacks/errors';
import type { ContentPackManifest, ManifestAudio } from '@/domain/contentPacks/types';

export function uniqueManifestAudio(manifest: ContentPackManifest): ManifestAudio[] {
  const seen = new Set<string>();
  const files: ManifestAudio[] = [];
  for (const lesson of manifest.lessons) {
    for (const audio of lesson.audio) {
      if (seen.has(audio.audioRecordingId)) {
        continue;
      }

      seen.add(audio.audioRecordingId);
      files.push(audio);
    }
  }

  return files;
}

export function downloadPercent(bytesDownloaded: number, bytesTotal: number): number {
  if (bytesTotal <= 0) {
    return 100;
  }

  return Math.min(100, Math.round((100 * bytesDownloaded) / bytesTotal));
}

export function applyRangeChunk(existing: Uint8Array, chunk: Uint8Array, startByte: number): Uint8Array {
  if (!Number.isInteger(startByte) || startByte < 0) {
    throw new ContentPackDownloadError('Invalid download offset.');
  }

  if (startByte > existing.length) {
    throw new ContentPackDownloadError('Download offset is past the local file.');
  }

  const next = new Uint8Array(startByte + chunk.length);
  next.set(existing.subarray(0, startByte), 0);
  next.set(chunk, startByte);
  return next;
}
