import { MAX_SYNC_ATTEMPTS } from '@/constants/sync';
import type { EntityId } from '@/domain/entities';
import { withStore } from '@/lib/database';
import type { LocalStore, SqlValue } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';

export { MAX_SYNC_ATTEMPTS };

export const SyncQueueStatus = {
  Pending: 'pending',
  DeadLetter: 'dead_letter',
} as const;
export type SyncQueueStatus = (typeof SyncQueueStatus)[keyof typeof SyncQueueStatus];

export const SyncQueueEntityType = {
  Progress: 'progress',
  SpacedRepetition: 'spaced_repetition',
  Gamification: 'gamification',
  ExerciseResult: 'exercise_result',
} as const;
export type SyncQueueEntityType = (typeof SyncQueueEntityType)[keyof typeof SyncQueueEntityType];

export type SyncQueueItem = {
  id: EntityId;
  languageId: EntityId;
  userId: EntityId;
  clientOperationId: string;
  entityType: SyncQueueEntityType;
  payload: string;
  attempts: number;
  maxAttempts: number;
  nextAttemptAt: string | null;
  status: SyncQueueStatus;
  lastError: string | null;
  createdAt: string;
  updatedAt: string;
};

type SyncQueueRow = {
  id: string;
  language_id: string;
  user_id: string;
  client_operation_id: string;
  entity_type: string;
  payload: string;
  attempts: number;
  max_attempts: number;
  next_attempt_at: string | null;
  status: string;
  last_error: string | null;
  created_at: string;
  updated_at: string;
};

function mapItem(row: SyncQueueRow): SyncQueueItem {
  return {
    id: row.id,
    languageId: row.language_id,
    userId: row.user_id,
    clientOperationId: row.client_operation_id,
    entityType: row.entity_type as SyncQueueEntityType,
    payload: row.payload,
    attempts: row.attempts,
    maxAttempts: row.max_attempts,
    nextAttemptAt: row.next_attempt_at,
    status: row.status as SyncQueueStatus,
    lastError: row.last_error,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

const ENQUEUE_SQL = `
INSERT INTO sync_queue (
  id, language_id, user_id, client_operation_id, entity_type, payload, attempts,
  max_attempts, next_attempt_at, status, last_error, created_at, updated_at
) VALUES (?, ?, ?, ?, ?, ?, 0, ?, NULL, ?, NULL, ?, ?)
ON CONFLICT(language_id, user_id, client_operation_id) DO NOTHING
`;

export async function enqueueSync(
  languageId: EntityId,
  item: Omit<SyncQueueItem, 'attempts' | 'maxAttempts' | 'nextAttemptAt' | 'status' | 'lastError'>,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, item.languageId);
  const params: SqlValue[] = [
    item.id,
    tenant,
    item.userId,
    item.clientOperationId,
    item.entityType,
    item.payload,
    MAX_SYNC_ATTEMPTS,
    SyncQueueStatus.Pending,
    item.createdAt,
    item.updatedAt,
  ];
  await withStore(store, (db) => db.run(ENQUEUE_SQL, params));
}

export async function listPendingSync(
  languageId: EntityId,
  userId: EntityId,
  asOf: string,
  store?: LocalStore,
): Promise<SyncQueueItem[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<SyncQueueRow>(
      `SELECT * FROM sync_queue
       WHERE language_id = ? AND user_id = ? AND status = ?
         AND (next_attempt_at IS NULL OR next_attempt_at <= ?)
       ORDER BY created_at`,
      [tenant, userId, SyncQueueStatus.Pending, asOf],
    );
    return rows.map(mapItem);
  });
}

export async function countDeadLetterSync(
  languageId: EntityId,
  userId: EntityId,
  store?: LocalStore,
): Promise<number> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const row = await db.getFirst<{ n: number }>(
      `SELECT COUNT(*) AS n FROM sync_queue
       WHERE language_id = ? AND user_id = ? AND status = ?`,
      [tenant, userId, SyncQueueStatus.DeadLetter],
    );
    return row?.n ?? 0;
  });
}

export async function markSyncAttempt(
  languageId: EntityId,
  id: EntityId,
  input: {
    succeeded: boolean;
    utcNow: string;
    error?: string;
    nextAttemptAt?: string | null;
    forceDeadLetter?: boolean;
  },
  store?: LocalStore,
): Promise<void> {
  const tenant = requireLanguageId(languageId);
  await withStore(store, async (db) => {
    if (input.succeeded) {
      await db.run(`DELETE FROM sync_queue WHERE id = ? AND language_id = ?`, [id, tenant]);
      return;
    }

    const row = await db.getFirst<SyncQueueRow>(
      `SELECT * FROM sync_queue WHERE id = ? AND language_id = ?`,
      [id, tenant],
    );
    if (!row) {
      return;
    }

    const attempts = row.attempts + 1;
    const dead = input.forceDeadLetter === true || attempts >= row.max_attempts;
    await db.run(
      `UPDATE sync_queue SET
         attempts = ?,
         status = ?,
         last_error = ?,
         next_attempt_at = ?,
         updated_at = ?
       WHERE id = ? AND language_id = ?`,
      [
        attempts,
        dead ? SyncQueueStatus.DeadLetter : SyncQueueStatus.Pending,
        input.error ?? null,
        dead ? null : (input.nextAttemptAt ?? input.utcNow),
        input.utcNow,
        id,
        tenant,
      ],
    );
  });
}
