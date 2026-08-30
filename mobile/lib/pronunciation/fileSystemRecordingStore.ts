import * as FileSystem from 'expo-file-system/legacy';

import { LOCAL_PRONUNCIATION_DIRECTORY } from '@/constants/pronunciation';
import { PronunciationRecordingError } from '@/domain/pronunciation/errors';
import type { LocalRecordingStore } from '@/domain/pronunciation/types';

function newRecordingId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `pronunciation-${Date.now()}`;
}

export function createFileSystemRecordingStore(): LocalRecordingStore {
  return {
    async persist(languageId: string, tempUri: string): Promise<string> {
      const root = FileSystem.documentDirectory;
      if (!root) {
        throw new PronunciationRecordingError('Local storage is not available on this device.');
      }

      const directory = `${root}${LOCAL_PRONUNCIATION_DIRECTORY}/${languageId}`;
      await FileSystem.makeDirectoryAsync(directory, { intermediates: true });
      const destination = `${directory}/${newRecordingId()}.m4a`;
      await FileSystem.moveAsync({ from: tempUri, to: destination });
      return destination;
    },
  };
}
