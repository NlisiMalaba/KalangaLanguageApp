import { newSyncId } from '@/domain/progress/ids';
import type { EntityId } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';
import type { SyncCheckpoint } from '@/domain/sync/ports';

type CheckpointRow = {
  language_id: string;
  user_id: string;
  sync_version: number;
  last_synced_at: string;
};

export async function getSyncCheckpoint(
  languageId: EntityId,
  userId: EntityId,
  store?: LocalStore,
): Promise<SyncCheckpoint | null> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const row = await db.getFirst<CheckpointRow>(
      `SELECT language_id, user_id, sync_version, last_synced_at
       FROM sync_checkpoints WHERE language_id = ? AND user_id = ?`,
      [tenant, userId],
    );
    if (!row) {
      return null;
    }

    return {
      languageId: row.language_id,
      userId: row.user_id,
      syncVersion: row.sync_version,
      lastSyncedAt: row.last_synced_at,
    };
  });
}

export async function saveSyncCheckpoint(checkpoint: SyncCheckpoint, store?: LocalStore): Promise<void> {
  const tenant = requireTenantMatch(checkpoint.languageId, checkpoint.languageId);
  await withStore(store, (db) =>
    db.run(
      `INSERT INTO sync_checkpoints (id, language_id, user_id, sync_version, last_synced_at)
       VALUES (?, ?, ?, ?, ?)
       ON CONFLICT(user_id, language_id) DO UPDATE SET
         sync_version = excluded.sync_version,
         last_synced_at = excluded.last_synced_at`,
      [newSyncId(), tenant, checkpoint.userId, checkpoint.syncVersion, checkpoint.lastSyncedAt],
    ),
  );
}
