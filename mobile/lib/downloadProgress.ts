import type { EntityId } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';

export const DownloadProgressStatus = {
  Pending: 'pending',
  InProgress: 'in_progress',
  Paused: 'paused',
  Complete: 'complete',
  Failed: 'failed',
} as const;
export type DownloadProgressStatus =
  (typeof DownloadProgressStatus)[keyof typeof DownloadProgressStatus];

export type DownloadProgress = {
  id: EntityId;
  languageId: EntityId;
  contentPackId: EntityId;
  recordingId: EntityId;
  bytesDownloaded: number;
  bytesTotal: number;
  localPath: string | null;
  status: DownloadProgressStatus;
  updatedAt: string;
};

type DownloadProgressRow = {
  id: string;
  language_id: string;
  content_pack_id: string;
  recording_id: string;
  bytes_downloaded: number;
  bytes_total: number;
  local_path: string | null;
  status: string;
  updated_at: string;
};

function mapRow(row: DownloadProgressRow): DownloadProgress {
  return {
    id: row.id,
    languageId: row.language_id,
    contentPackId: row.content_pack_id,
    recordingId: row.recording_id,
    bytesDownloaded: row.bytes_downloaded,
    bytesTotal: row.bytes_total,
    localPath: row.local_path,
    status: row.status as DownloadProgressStatus,
    updatedAt: row.updated_at,
  };
}

const UPSERT_SQL = `
INSERT INTO download_progress (
  id, language_id, content_pack_id, recording_id, bytes_downloaded, bytes_total,
  local_path, status, updated_at
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
ON CONFLICT(content_pack_id, recording_id) DO UPDATE SET
  bytes_downloaded = excluded.bytes_downloaded,
  bytes_total = excluded.bytes_total,
  local_path = excluded.local_path,
  status = excluded.status,
  updated_at = excluded.updated_at
WHERE download_progress.language_id = excluded.language_id
`;

export async function upsertDownloadProgress(
  languageId: EntityId,
  progress: DownloadProgress,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, progress.languageId);
  await withStore(store, (db) =>
    db.run(UPSERT_SQL, [
      progress.id,
      tenant,
      progress.contentPackId,
      progress.recordingId,
      progress.bytesDownloaded,
      progress.bytesTotal,
      progress.localPath,
      progress.status,
      progress.updatedAt,
    ]),
  );
}

export async function getDownloadProgress(
  languageId: EntityId,
  contentPackId: EntityId,
  recordingId: EntityId,
  store?: LocalStore,
): Promise<DownloadProgress | null> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const row = await db.getFirst<DownloadProgressRow>(
      `SELECT * FROM download_progress
       WHERE language_id = ? AND content_pack_id = ? AND recording_id = ?`,
      [tenant, contentPackId, recordingId],
    );
    return row ? mapRow(row) : null;
  });
}

export async function listDownloadProgressForPack(
  languageId: EntityId,
  contentPackId: EntityId,
  store?: LocalStore,
): Promise<DownloadProgress[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<DownloadProgressRow>(
      `SELECT * FROM download_progress WHERE language_id = ? AND content_pack_id = ?`,
      [tenant, contentPackId],
    );
    return rows.map(mapRow);
  });
}

export async function listDownloadProgressForLanguage(
  languageId: EntityId,
  store?: LocalStore,
): Promise<DownloadProgress[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<DownloadProgressRow>(
      `SELECT * FROM download_progress WHERE language_id = ?`,
      [tenant],
    );
    return rows.map(mapRow);
  });
}
