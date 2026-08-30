import { ContentPackNotFoundError } from '@/domain/contentPacks/errors';
import { exclusiveLessonIds, summarizeStorage } from '@/domain/contentPacks/storage';
import type { StorageSummary } from '@/domain/contentPacks/storage';
import type { ContentPackListItem } from '@/domain/contentPacks/types';
import type { ContentPack, EntityId } from '@/domain/entities';

export type PackFileRef = {
  recordingId: EntityId;
  localPath: string | null;
};

export type StorageManagerDeps = {
  listLocalPacks: (languageId: EntityId) => Promise<ContentPack[]>;
  listRemotePacks: (languageId: EntityId) => Promise<ContentPackListItem[]>;
  getLocalPack: (languageId: EntityId, packId: EntityId) => Promise<ContentPack | null>;
  listPackFiles: (languageId: EntityId, packId: EntityId) => Promise<PackFileRef[]>;
  deleteAudioFile: (localPath: string) => Promise<void>;
  deletePackContent: (input: {
    languageId: EntityId;
    packId: EntityId;
    exclusiveLessonIds: readonly EntityId[];
  }) => Promise<void>;
};

export type StorageManager = {
  getSummary: (languageId: EntityId) => Promise<StorageSummary>;
  deletePack: (languageId: EntityId, packId: EntityId) => Promise<void>;
};

export function createStorageManager(deps: StorageManagerDeps): StorageManager {
  return {
    async getSummary(languageId) {
      const local = await deps.listLocalPacks(languageId);
      let remote: ContentPackListItem[] = [];
      try {
        remote = await deps.listRemotePacks(languageId);
      } catch {
        remote = [];
      }

      return summarizeStorage(local, remote);
    },

    async deletePack(languageId, packId) {
      const packs = await deps.listLocalPacks(languageId);
      const pack = packs.find((item) => item.id === packId) ?? (await deps.getLocalPack(languageId, packId));
      if (!pack) {
        throw new ContentPackNotFoundError(packId);
      }

      const lessons = exclusiveLessonIds(pack, packs);
      const files = await deps.listPackFiles(languageId, packId);
      for (const file of files) {
        if (file.localPath) {
          await deps.deleteAudioFile(file.localPath);
        }
      }

      await deps.deletePackContent({ languageId, packId, exclusiveLessonIds: lessons });
    },
  };
}
