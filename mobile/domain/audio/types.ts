import type { AudioRef } from '@/domain/catalog/types';
import type { EntityId } from '@/domain/entities';
import { PLAYBACK_RATE_NORMAL, PLAYBACK_RATE_SLOW } from '@/constants/audio';

export const PlaybackRate = {
  Normal: PLAYBACK_RATE_NORMAL,
  Slow: PLAYBACK_RATE_SLOW,
} as const;
export type PlaybackRate = (typeof PlaybackRate)[keyof typeof PlaybackRate];

export const AudioSourceKind = {
  Local: 'local',
  Cdn: 'cdn',
  Tts: 'tts',
} as const;
export type AudioSourceKind = (typeof AudioSourceKind)[keyof typeof AudioSourceKind];

export type ResolvedAudioSource =
  | { kind: typeof AudioSourceKind.Local; uri: string; recordingId: EntityId }
  | { kind: typeof AudioSourceKind.Cdn; uri: string; recordingId: EntityId }
  | { kind: typeof AudioSourceKind.Tts; text: string };

export type AudioPlaybackSession = {
  languageId: EntityId;
  recordings: readonly AudioRef[];
  ttsText?: string | null;
};

export type LocalAudioExists = {
  exists(uri: string): Promise<boolean>;
};

export type AudioPlaybackEngine = {
  load(uri: string, rate: PlaybackRate): Promise<void>;
  play(): Promise<void>;
  replay(): Promise<void>;
  setRate(rate: PlaybackRate): Promise<void>;
  stop(): Promise<void>;
  unload(): Promise<void>;
};

export type TtsEngine = {
  speak(text: string, rate: PlaybackRate): Promise<void>;
  stop(): Promise<void>;
};

export type AudioPlayerDeps = {
  localAudioExists: LocalAudioExists;
  documentDirectory: () => string;
  playback: AudioPlaybackEngine;
  tts: TtsEngine;
};
