import type { ContentPack, EntityId } from '@/domain/entities';
import type { ContentPackListItem } from '@/domain/contentPacks/types';

export type StoredPackUsage = {
  packId: EntityId;
  name: string;
  sizeBytes: number;
  version: number;
  updateAvailable: boolean;
  remoteVersion: number | null;
};

export type StorageSummary = {
  totalBytes: number;
  packs: StoredPackUsage[];
};

export function totalStorageBytes(packs: readonly { sizeBytes: number }[]): number {
  return packs.reduce((sum, pack) => sum + Math.max(0, pack.sizeBytes), 0);
}

export function exclusiveLessonIds(
  pack: ContentPack,
  others: readonly ContentPack[],
): EntityId[] {
  const shared = new Set<EntityId>();
  for (const other of others) {
    if (other.id === pack.id) {
      continue;
    }

    for (const lessonId of other.lessonIds) {
      shared.add(lessonId);
    }
  }

  return pack.lessonIds.filter((lessonId) => !shared.has(lessonId));
}

export function withUpdateFlags(
  local: readonly ContentPack[],
  remote: readonly ContentPackListItem[],
): StoredPackUsage[] {
  const remoteById = new Map(remote.map((pack) => [pack.packId, pack]));
  return local.map((pack) => {
    const listed = remoteById.get(pack.id);
    const remoteVersion = listed?.version ?? null;
    return {
      packId: pack.id,
      name: pack.name,
      sizeBytes: pack.sizeBytes,
      version: pack.version,
      remoteVersion,
      updateAvailable: remoteVersion != null && remoteVersion > pack.version,
    };
  });
}

export function summarizeStorage(
  local: readonly ContentPack[],
  remote: readonly ContentPackListItem[] = [],
): StorageSummary {
  return {
    totalBytes: totalStorageBytes(local),
    packs: withUpdateFlags(local, remote),
  };
}

export function formatStorageBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${Math.round((bytes / 1024) * 10) / 10} KB`;
  }

  return `${Math.round((bytes / (1024 * 1024)) * 10) / 10} MB`;
}
