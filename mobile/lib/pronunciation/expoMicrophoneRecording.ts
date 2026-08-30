import { Audio } from 'expo-av';

import { PronunciationRecordingError } from '@/domain/pronunciation/errors';
import type { Microphone, MicrophonePermissionStatus, RecordingSession } from '@/domain/pronunciation/types';

function mapPermission(status: string | undefined, granted: boolean): MicrophonePermissionStatus {
  if (granted || status === 'granted') {
    return 'granted';
  }

  if (status === 'undetermined') {
    return 'undetermined';
  }

  return 'denied';
}

export function createExpoMicrophone(): Microphone {
  return {
    async getPermission() {
      const result = await Audio.getPermissionsAsync();
      return mapPermission(result.status, result.granted);
    },
    async requestPermission() {
      const result = await Audio.requestPermissionsAsync();
      return mapPermission(result.status, result.granted);
    },
  };
}

export function createExpoRecordingSession(): RecordingSession {
  let recording: Audio.Recording | null = null;
  let onMetering: ((db: number) => void) | undefined;

  return {
    async start({ onMetering: nextMetering }) {
      if (recording) {
        throw new PronunciationRecordingError('A pronunciation recording is already in progress.');
      }

      onMetering = nextMetering;
      await Audio.setAudioModeAsync({
        allowsRecordingIOS: true,
        playsInSilentModeIOS: true,
        staysActiveInBackground: false,
      });

      const next = new Audio.Recording();
      await next.prepareToRecordAsync({
        ...Audio.RecordingOptionsPresets.HIGH_QUALITY,
        isMeteringEnabled: true,
      });
      next.setOnRecordingStatusUpdate((status) => {
        if (status.isRecording && typeof status.metering === 'number') {
          onMetering?.(status.metering);
        }
      });
      await next.startAsync();
      recording = next;
    },
    async stop() {
      if (!recording) {
        throw new PronunciationRecordingError('No pronunciation recording is in progress.');
      }

      const status = await recording.stopAndUnloadAsync();
      const tempUri = recording.getURI();
      recording.setOnRecordingStatusUpdate(null);
      recording = null;
      onMetering = undefined;

      if (!tempUri) {
        throw new PronunciationRecordingError('The recorder did not produce a local file.');
      }

      const durationMs = typeof status.durationMillis === 'number' ? status.durationMillis : 0;

      return { tempUri, durationMs };
    },
  };
}
