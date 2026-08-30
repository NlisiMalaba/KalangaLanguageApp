import { createStorageManager } from '@/domain/contentPacks/storageManager';
import type { StorageManagerDeps } from '@/domain/contentPacks/storageManager';
import { getContentPack, listContentPacks } from '@/lib/contentPacks';
import { deletePackContent } from '@/lib/contentPacks/deletePackContent';
import { expoDeleteAudioFile } from '@/lib/contentPacks/expoContentPackFiles';
import { createHttpContentPackApi } from '@/lib/contentPacks/httpContentPackApi';
import { listDownloadProgressForPack } from '@/lib/downloadProgress';
import type { LocalStore } from '@/lib/localStore';

export function createDefaultStorageManager(
  overrides: Partial<StorageManagerDeps> & { store?: LocalStore } = {},
) {
  const store = overrides.store;
  const api = createHttpContentPackApi();
  const deps: StorageManagerDeps = {
    listLocalPacks: (languageId) => listContentPacks(languageId, store),
    listRemotePacks: (languageId) => api.listPacks(languageId),
    getLocalPack: (languageId, packId) => getContentPack(languageId, packId, store),
    listPackFiles: async (languageId, packId) => {
      const rows = await listDownloadProgressForPack(languageId, packId, store);
      return rows.map((row) => ({ recordingId: row.recordingId, localPath: row.localPath }));
    },
    deleteAudioFile: expoDeleteAudioFile,
    deletePackContent: (input) => deletePackContent(input, store),
    ...overrides,
  };

  return createStorageManager(deps);
}
