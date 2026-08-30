import * as Speech from 'expo-speech';

import { PLAYBACK_RATE_NORMAL } from '@/constants/audio';
import type { PlaybackRate, TtsEngine } from '@/domain/audio/types';

export function createExpoSpeechEngine(): TtsEngine {
  return {
    async speak(text: string, rate: PlaybackRate): Promise<void> {
      await Speech.stop();
      await new Promise<void>((resolve, reject) => {
        Speech.speak(text, {
          rate: rate / PLAYBACK_RATE_NORMAL,
          onDone: () => resolve(),
          onStopped: () => resolve(),
          onError: () => reject(new Error('Text-to-speech failed.')),
        });
      });
    },
    async stop(): Promise<void> {
      await Speech.stop();
    },
  };
}
