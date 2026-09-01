import { ALLOWED_AUDIO_MIME, MAX_AUDIO_UPLOAD_BYTES } from '@/constants/contributor';
import { ContributorValidationError } from '@/domain/contributor/errors';
import type { PickedAudioFile } from '@/domain/contributor/types';
import * as DocumentPicker from 'expo-document-picker';
import { Audio } from 'expo-av';

export async function pickAudioFile(): Promise<PickedAudioFile | null> {
  const result = await DocumentPicker.getDocumentAsync({
    type: ['audio/mpeg', 'audio/mp4', 'audio/aac', 'audio/x-m4a', 'audio/*'],
    copyToCacheDirectory: true,
  });
  if (result.canceled || !result.assets[0]) {
    return null;
  }

  const asset = result.assets[0];
  const mimeType = (asset.mimeType ?? 'audio/mpeg').toLowerCase();
  if (asset.size != null && asset.size > MAX_AUDIO_UPLOAD_BYTES) {
    throw new ContributorValidationError('Audio must be an MP3 or AAC file of 10 MB or less.');
  }

  const allowed = ALLOWED_AUDIO_MIME.some((type) => mimeType === type || mimeType.includes('mpeg') || mimeType.includes('mp3') || mimeType.includes('aac') || mimeType.includes('m4a') || mimeType === 'audio/mp4');
  if (!allowed) {
    throw new ContributorValidationError('Audio must be an MP3 or AAC file of 10 MB or less.');
  }

  return {
    uri: asset.uri,
    name: asset.name ?? 'audio.mp3',
    mimeType,
    sizeBytes: asset.size ?? null,
  };
}

export async function readAudioDurationMs(uri: string): Promise<number> {
  const { sound, status } = await Audio.Sound.createAsync({ uri });
  try {
    const duration = status.isLoaded && status.durationMillis ? status.durationMillis : 1000;
    return Math.max(1, duration);
  } finally {
    await sound.unloadAsync();
  }
}
