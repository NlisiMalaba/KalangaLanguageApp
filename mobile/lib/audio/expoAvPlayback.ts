import { Audio } from 'expo-av';

import { AudioPlaybackError } from '@/domain/audio/errors';
import type { AudioPlaybackEngine, PlaybackRate } from '@/domain/audio/types';

export function createExpoAvPlaybackEngine(): AudioPlaybackEngine {
  let sound: Audio.Sound | null = null;

  async function requireSound(): Promise<Audio.Sound> {
    if (!sound) {
      throw new AudioPlaybackError('No audio is loaded.');
    }

    return sound;
  }

  return {
    async load(uri: string, rate: PlaybackRate): Promise<void> {
      await Audio.setAudioModeAsync({
        allowsRecordingIOS: false,
        playsInSilentModeIOS: true,
        staysActiveInBackground: false,
      });

      if (sound) {
        await sound.unloadAsync();
        sound = null;
      }

      const next = new Audio.Sound();
      await next.loadAsync(
        { uri },
        {
          shouldPlay: false,
          rate,
          shouldCorrectPitch: true,
          progressUpdateIntervalMillis: 250,
        },
      );
      sound = next;
    },
    async play(): Promise<void> {
      const current = await requireSound();
      await current.playAsync();
    },
    async replay(): Promise<void> {
      const current = await requireSound();
      await current.setPositionAsync(0);
      await current.playAsync();
    },
    async setRate(rate: PlaybackRate): Promise<void> {
      if (!sound) {
        return;
      }

      await sound.setRateAsync(rate, true);
    },
    async stop(): Promise<void> {
      if (!sound) {
        return;
      }

      await sound.stopAsync();
    },
    async unload(): Promise<void> {
      if (!sound) {
        return;
      }

      await sound.unloadAsync();
      sound = null;
    },
  };
}
