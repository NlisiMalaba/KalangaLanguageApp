import * as FileSystem from 'expo-file-system/legacy';

import type { LocalAudioExists } from '@/domain/audio/types';

export function expoDocumentDirectory(): string {
  return FileSystem.documentDirectory ?? 'file:///';
}

export function createExpoLocalAudioExists(): LocalAudioExists {
  return {
    async exists(uri: string): Promise<boolean> {
      const info = await FileSystem.getInfoAsync(uri);
      return info.exists === true;
    },
  };
}
