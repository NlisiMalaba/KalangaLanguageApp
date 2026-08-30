import type { EntityId, Instant } from '@/domain/entities';
import type { Level } from '@/domain/enums';

export type SyncProgressWire = {
  lessonId: EntityId;
  completedAt: Instant | null;
  score: number | null;
  xpAwarded: number;
  updatedAt: Instant;
};

export type SyncSrsWire = {
  phraseId: EntityId;
  variationId: EntityId | null;
  easeFactor: number;
  intervalDays: number;
  repetitions: number;
  nextReviewAt: string;
  lastReviewedAt: Instant | null;
  updatedAt: Instant;
};

export type SyncGamificationWire = {
  totalXp: number;
  currentStreak: number;
  longestStreak: number;
  lastActivityDate: string | null;
  progressLevel?: Level;
  xpDelta: number;
  updatedAt: Instant;
};

export type SyncChanges = {
  progress: SyncProgressWire[];
  spacedRepetition: SyncSrsWire[];
  gamification: SyncGamificationWire | null;
};

export type SyncPushRequest = {
  clientOperationId: string;
  progress: SyncProgressWire[];
  spacedRepetition: SyncSrsWire[];
  gamification: SyncGamificationWire | null;
};

export type SyncPushResult = {
  syncVersion: number;
  lastSyncedAt: Instant;
  idempotentReplay: boolean;
  serverChanges: SyncChanges;
};

export type SyncStatus = {
  lastSyncedAt: Instant | null;
  syncVersion: number;
  deadLetterCount: number;
};

export type SyncRunResult = {
  pushed: number;
  failed: number;
  deadLettered: number;
  skippedOffline: boolean;
};
