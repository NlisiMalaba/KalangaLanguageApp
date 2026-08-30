import { AudioUnavailableError, AudioValidationError } from '@/domain/audio/errors';
import { localAudioFileUri } from '@/domain/audio/localAudioPath';
import {
  AudioSourceKind,
  type AudioPlaybackSession,
  type LocalAudioExists,
  type ResolvedAudioSource,
} from '@/domain/audio/types';
import type { AudioRef } from '@/domain/catalog/types';
import type { EntityId } from '@/domain/entities';

function requireId(value: string, field: string): EntityId {
  const id = value.trim();
  if (id.length === 0) {
    throw new AudioValidationError(`${field} is required.`);
  }

  return id;
}

export type ResolveAudioSourceInput = AudioPlaybackSession & {
  recordingId?: EntityId | null;
};

export async function resolveAudioSource(
  input: ResolveAudioSourceInput,
  deps: { localAudioExists: LocalAudioExists; documentDirectory: () => string },
): Promise<ResolvedAudioSource> {
  const languageId = requireId(input.languageId, 'language_id');
  const recordings = input.recordings;
  const recording = selectRecording(recordings, input.recordingId);
  const ttsText = input.ttsText?.trim() ? input.ttsText.trim() : null;

  if (recording) {
    const localUri = localAudioFileUri(
      deps.documentDirectory(),
      languageId,
      recording.id,
      recording.fileFormat,
    );
    if (await deps.localAudioExists.exists(localUri)) {
      return { kind: AudioSourceKind.Local, uri: localUri, recordingId: recording.id };
    }

    const cdnUrl = recording.cdnUrl.trim();
    if (cdnUrl.length > 0) {
      return { kind: AudioSourceKind.Cdn, uri: cdnUrl, recordingId: recording.id };
    }
  }

  if (ttsText) {
    return { kind: AudioSourceKind.Tts, text: ttsText };
  }

  throw new AudioUnavailableError();
}

function selectRecording(
  recordings: readonly AudioRef[],
  recordingId: EntityId | null | undefined,
): AudioRef | null {
  if (recordings.length === 0) {
    return null;
  }

  if (!recordingId) {
    return recordings[0] ?? null;
  }

  return recordings.find((item) => item.id === recordingId) ?? null;
}
