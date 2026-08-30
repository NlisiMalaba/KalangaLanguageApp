import { MicrophonePermissionDeniedError, PronunciationRecordingError } from '@/domain/pronunciation/errors';
import type {
  AppSettingsOpener,
  LocalPronunciationRecording,
  LocalRecordingStore,
  Microphone,
  RecordingSession,
} from '@/domain/pronunciation/types';

export type PronunciationRecorderDeps = {
  microphone: Microphone;
  recording: RecordingSession;
  store: LocalRecordingStore;
  settings: AppSettingsOpener;
};

export type PronunciationRecorder = {
  ensurePermission(): Promise<void>;
  openSettings(): Promise<void>;
  start(languageId: string): Promise<void>;
  stop(): Promise<LocalPronunciationRecording>;
};

function meteringToEnergy(db: number): number {
  const clamped = Math.min(0, Math.max(-60, db));
  return (clamped + 60) / 60;
}

export function createPronunciationRecorder(deps: PronunciationRecorderDeps): PronunciationRecorder {
  let languageId: string | null = null;
  let recording = false;
  const energyProfile: number[] = [];

  return {
    async ensurePermission() {
      const current = await deps.microphone.getPermission();
      if (current === 'granted') {
        return;
      }

      const requested = current === 'undetermined' ? await deps.microphone.requestPermission() : current;
      if (requested !== 'granted') {
        throw new MicrophonePermissionDeniedError();
      }
    },
    openSettings() {
      return deps.settings.openSettings();
    },
    async start(nextLanguageId: string) {
      const tenant = nextLanguageId.trim();
      if (tenant.length === 0) {
        throw new PronunciationRecordingError('language_id is required.');
      }

      await this.ensurePermission();
      if (recording) {
        throw new PronunciationRecordingError('A pronunciation recording is already in progress.');
      }

      energyProfile.length = 0;
      languageId = tenant;
      await deps.recording.start({
        languageId: tenant,
        onMetering: (db) => {
          energyProfile.push(meteringToEnergy(db));
        },
      });
      recording = true;
    },
    async stop() {
      if (!recording || !languageId) {
        throw new PronunciationRecordingError('No pronunciation recording is in progress.');
      }

      const stopped = await deps.recording.stop();
      recording = false;
      const uri = await deps.store.persist(languageId, stopped.tempUri);
      const result: LocalPronunciationRecording = {
        uri,
        durationMs: Math.max(0, stopped.durationMs),
        energyProfile: [...energyProfile],
      };
      energyProfile.length = 0;
      languageId = null;
      return result;
    },
  };
}
