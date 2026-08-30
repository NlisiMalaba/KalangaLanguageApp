import { LOCAL_AUDIO_DIRECTORY } from '@/constants/audio';
import type { EntityId } from '@/domain/entities';

export function extensionForAudioFormat(fileFormat: string): string {
  const normalized = fileFormat.trim().toLowerCase();
  if (normalized === 'aac') {
    return 'aac';
  }

  return 'mp3';
}

export function localAudioRelativePath(
  languageId: EntityId,
  recordingId: EntityId,
  fileFormat: string,
): string {
  return `${LOCAL_AUDIO_DIRECTORY}/${languageId}/${recordingId}.${extensionForAudioFormat(fileFormat)}`;
}

export function localAudioFileUri(
  documentDirectory: string,
  languageId: EntityId,
  recordingId: EntityId,
  fileFormat: string,
): string {
  const base = documentDirectory.endsWith('/') ? documentDirectory : `${documentDirectory}/`;
  return `${base}${localAudioRelativePath(languageId, recordingId, fileFormat)}`;
}
