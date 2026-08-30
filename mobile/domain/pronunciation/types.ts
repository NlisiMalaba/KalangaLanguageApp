import {
  PRONUNCIATION_EXCELLENT_MIN,
  PRONUNCIATION_GOOD_MIN,
} from '@/constants/pronunciation';

export const PronunciationLabel = {
  Excellent: 'Excellent',
  Good: 'Good',
  TryAgain: 'Try again',
} as const;
export type PronunciationLabel = (typeof PronunciationLabel)[keyof typeof PronunciationLabel];

export type AudioSample = {
  durationMs: number;
  energyProfile: readonly number[];
};

export type PronunciationScore = {
  score: number;
  label: PronunciationLabel;
};

export type MicrophonePermissionStatus = 'granted' | 'denied' | 'undetermined';

export type LocalPronunciationRecording = {
  uri: string;
  durationMs: number;
  energyProfile: readonly number[];
};

export type Microphone = {
  getPermission(): Promise<MicrophonePermissionStatus>;
  requestPermission(): Promise<MicrophonePermissionStatus>;
};

export type AppSettingsOpener = {
  openSettings(): Promise<void>;
};

export type RecordingSession = {
  start(options: { languageId: string; onMetering?: (db: number) => void }): Promise<void>;
  stop(): Promise<{ tempUri: string; durationMs: number }>;
};

export type LocalRecordingStore = {
  persist(languageId: string, tempUri: string): Promise<string>;
};

export type PronunciationTransmitter = {
  upload(localUri: string): Promise<void>;
};

export function labelForScore(score: number): PronunciationLabel {
  if (score >= PRONUNCIATION_EXCELLENT_MIN) {
    return PronunciationLabel.Excellent;
  }

  if (score >= PRONUNCIATION_GOOD_MIN) {
    return PronunciationLabel.Good;
  }

  return PronunciationLabel.TryAgain;
}
