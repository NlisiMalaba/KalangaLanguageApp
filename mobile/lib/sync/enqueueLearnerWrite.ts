import { newSyncId } from '@/domain/progress/ids';
import type { EntityId } from '@/domain/entities';
import { enqueueSync, type SyncQueueEntityType } from '@/lib/syncQueue';
import type { LocalStore } from '@/lib/localStore';

export async function enqueueLearnerWrite(
  input: {
    languageId: EntityId;
    userId: EntityId;
    entityType: SyncQueueEntityType;
    clientOperationId: string;
    payload: unknown;
  },
  store?: LocalStore,
): Promise<void> {
  const instant = new Date().toISOString();
  await enqueueSync(
    input.languageId,
    {
      id: newSyncId(),
      languageId: input.languageId,
      userId: input.userId,
      clientOperationId: input.clientOperationId,
      entityType: input.entityType,
      payload: JSON.stringify(input.payload),
      createdAt: instant,
      updatedAt: instant,
    },
    store,
  );
}
