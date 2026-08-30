import { createPronunciationRecorder } from '@/domain/pronunciation/pronunciationRecorder';
import type { PronunciationRecorderDeps } from '@/domain/pronunciation/pronunciationRecorder';
import { createExpoAppSettingsOpener } from '@/lib/pronunciation/openAppSettings';
import { createExpoMicrophone, createExpoRecordingSession } from '@/lib/pronunciation/expoMicrophoneRecording';
import { createFileSystemRecordingStore } from '@/lib/pronunciation/fileSystemRecordingStore';

export function createDefaultPronunciationRecorder(overrides: Partial<PronunciationRecorderDeps> = {}) {
  return createPronunciationRecorder({
    microphone: createExpoMicrophone(),
    recording: createExpoRecordingSession(),
    store: createFileSystemRecordingStore(),
    settings: createExpoAppSettingsOpener(),
    ...overrides,
  });
}
