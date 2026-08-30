import { createContentPackDownloader } from '@/domain/contentPacks/downloadContentPackUseCase';
import type { ContentPackDownloaderDeps } from '@/domain/contentPacks/downloadContentPackUseCase';
import type { PackDownloadFileState } from '@/domain/contentPacks/types';
import { upsertContentPack } from '@/lib/contentPacks';
import { createHttpContentPackApi } from '@/lib/contentPacks/httpContentPackApi';
import { fetchByteRange } from '@/lib/contentPacks/fetchByteRange';
import {
  expoDocumentDirectory,
  expoEnsureDirectory,
  expoFileSize,
  expoFreeDiskBytes,
  expoReadFileBytes,
  expoWriteFileBytes,
} from '@/lib/contentPacks/expoContentPackFiles';
import {
  getDownloadProgress,
  listDownloadProgressForPack,
  upsertDownloadProgress,
  type DownloadProgress,
  type DownloadProgressStatus,
} from '@/lib/downloadProgress';
import type { LocalStore } from '@/lib/localStore';

function toSqlite(row: PackDownloadFileState): DownloadProgress {
  return {
    ...row,
    status: row.status as DownloadProgressStatus,
  };
}

export function createDefaultContentPackDownloader(
  overrides: Partial<ContentPackDownloaderDeps> & { store?: LocalStore } = {},
) {
  const store = overrides.store;
  const api = createHttpContentPackApi();
  const deps: ContentPackDownloaderDeps = {
    getManifest: (languageId, packId) => api.getManifest(languageId, packId),
    getFreeDiskBytes: expoFreeDiskBytes,
    documentDirectory: expoDocumentDirectory,
    ensureDirectory: expoEnsureDirectory,
    fileSize: expoFileSize,
    readFile: expoReadFileBytes,
    writeFile: expoWriteFileBytes,
    fetchRange: fetchByteRange,
    getProgress: async (languageId, packId, recordingId) => {
      const row = await getDownloadProgress(languageId, packId, recordingId, store);
      return row;
    },
    upsertProgress: (languageId, progress) => upsertDownloadProgress(languageId, toSqlite(progress), store),
    listProgress: (languageId, packId) => listDownloadProgressForPack(languageId, packId, store),
    upsertPack: (languageId, pack) => upsertContentPack(languageId, pack, store),
    ...overrides,
  };

  return createContentPackDownloader(deps);
}
