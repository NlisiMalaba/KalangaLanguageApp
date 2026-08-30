import type { EntityId } from '@/domain/entities';
import type { SyncChanges, SyncPushRequest, SyncPushResult } from '@/domain/sync/types';

export type PendingSyncItem = {
  id: EntityId;
  languageId: EntityId;
  userId: EntityId;
  clientOperationId: string;
  entityType: 'progress' | 'spaced_repetition' | 'gamification' | 'exercise_result';
  payload: string;
  attempts: number;
  maxAttempts: number;
};

export type SyncCheckpoint = {
  languageId: EntityId;
  userId: EntityId;
  syncVersion: number;
  lastSyncedAt: string;
};

export type SyncServiceDeps = {
  isOnline: () => Promise<boolean>;
  listPending: (languageId: EntityId, userId: EntityId, asOf: string) => Promise<PendingSyncItem[]>;
  markAttempt: (input: {
    languageId: EntityId;
    id: EntityId;
    succeeded: boolean;
    utcNow: string;
    error?: string;
    nextAttemptAt?: string | null;
    forceDeadLetter?: boolean;
  }) => Promise<void>;
  push: (request: SyncPushRequest) => Promise<SyncPushResult>;
  pull: (sinceVersion: number) => Promise<SyncPushResult>;
  applyChanges: (languageId: EntityId, userId: EntityId, changes: SyncChanges) => Promise<void>;
  getCheckpoint: (languageId: EntityId, userId: EntityId) => Promise<SyncCheckpoint | null>;
  saveCheckpoint: (checkpoint: SyncCheckpoint) => Promise<void>;
  countDeadLetters: (languageId: EntityId, userId: EntityId) => Promise<number>;
  now?: () => Date;
  random?: () => number;
};
