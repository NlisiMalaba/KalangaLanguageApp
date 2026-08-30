import type { EntityId } from '@/domain/entities';
import type { Level } from '@/domain/enums';

export type ContentPackListItem = {
  packId: EntityId;
  languageId: EntityId;
  name: string;
  level: Level | null;
  category: string | null;
  version: number;
  sizeBytes: number;
  manifestUrl: string;
};

export type ManifestAudio = {
  audioRecordingId: EntityId;
  cdnUrl: string;
  fileFormat: string;
  fileSizeBytes: number;
};

export type ContentPackManifest = {
  packId: EntityId;
  languageId: EntityId;
  name: string;
  version: number;
  level: Level | null;
  category: string | null;
  generatedAt: string;
  sizeBytes: number;
  lessons: { lessonId: EntityId; audio: ManifestAudio[] }[];
};

export type PackDownloadProgress = {
  percent: number;
  bytesDownloaded: number;
  bytesTotal: number;
  pausedForStorage: boolean;
};

export type DownloadContentPackInput = {
  languageId: EntityId;
  packId: EntityId;
  onProgress?: (progress: PackDownloadProgress) => void;
};

export type PackDownloadFileState = {
  id: EntityId;
  languageId: EntityId;
  contentPackId: EntityId;
  recordingId: EntityId;
  bytesDownloaded: number;
  bytesTotal: number;
  localPath: string | null;
  status: 'pending' | 'in_progress' | 'paused' | 'complete' | 'failed';
  updatedAt: string;
};

export type DownloadContentPackResult = {
  packId: EntityId;
  percent: number;
  pausedForStorage: boolean;
  filesCompleted: number;
  filesTotal: number;
};
