import { createAudioPlayer } from '@/domain/audio/audioPlayer';
import type { AudioPlayerDeps } from '@/domain/audio/types';
import { createExpoAvPlaybackEngine } from '@/lib/audio/expoAvPlayback';
import { createExpoLocalAudioExists, expoDocumentDirectory } from '@/lib/audio/expoLocalAudio';
import { createExpoSpeechEngine } from '@/lib/audio/expoSpeech';

export function createDefaultAudioPlayer(overrides: Partial<AudioPlayerDeps> = {}) {
  const deps: AudioPlayerDeps = {
    localAudioExists: createExpoLocalAudioExists(),
    documentDirectory: expoDocumentDirectory,
    playback: createExpoAvPlaybackEngine(),
    tts: createExpoSpeechEngine(),
    ...overrides,
  };

  return createAudioPlayer(deps);
}
