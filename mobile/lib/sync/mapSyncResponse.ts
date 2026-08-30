import { Level } from '@/domain/enums';
import type { EntityId } from '@/domain/entities';
import { SyncError } from '@/domain/sync/errors';
import type { SyncChanges, SyncGamificationWire, SyncProgressWire, SyncPushResult, SyncSrsWire } from '@/domain/sync/types';
import { LEVEL_BY_NUMBER, type IdWire } from '@/lib/catalog/wire';

function unwrapId(value: IdWire, field: string): EntityId {
  if (typeof value === 'string' && value.length > 0) {
    return value;
  }

  if (value && typeof value === 'object' && typeof value.value === 'string' && value.value.length > 0) {
    return value.value;
  }

  throw new SyncError(`Sync response is missing ${field}.`);
}

function optionalId(value: IdWire): EntityId | null {
  if (value == null || value === '') {
    return null;
  }

  if (typeof value === 'string') {
    return value;
  }

  if (typeof value === 'object' && typeof value.value === 'string' && value.value.length > 0) {
    return value.value;
  }

  return null;
}

function requireString(value: unknown, field: string): string {
  if (typeof value !== 'string' || value.length === 0) {
    throw new SyncError(`Sync response is missing ${field}.`);
  }

  return value;
}

function optionalString(value: unknown): string | null {
  return typeof value === 'string' && value.length > 0 ? value : null;
}

function requireNumber(value: unknown, field: string): number {
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    throw new SyncError(`Sync response is missing ${field}.`);
  }

  return value;
}

function optionalNumber(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

function mapLevel(value: unknown): typeof Level[keyof typeof Level] {
  if (typeof value === 'string' && (Object.values(Level) as string[]).includes(value)) {
    return value as typeof Level[keyof typeof Level];
  }

  if (typeof value === 'number' && LEVEL_BY_NUMBER[value]) {
    return LEVEL_BY_NUMBER[value];
  }

  return Level.Beginner;
}

function mapProgress(item: Record<string, unknown>): SyncProgressWire {
  return {
    lessonId: unwrapId(item.lessonId as IdWire, 'lessonId'),
    completedAt: optionalString(item.completedAt),
    score: optionalNumber(item.score),
    xpAwarded: requireNumber(item.xpAwarded, 'xpAwarded'),
    updatedAt: requireString(item.updatedAt, 'updatedAt'),
  };
}

function mapSrs(item: Record<string, unknown>): SyncSrsWire {
  return {
    phraseId: unwrapId(item.phraseId as IdWire, 'phraseId'),
    variationId: optionalId(item.variationId as IdWire),
    easeFactor: requireNumber(item.easeFactor, 'easeFactor'),
    intervalDays: requireNumber(item.intervalDays, 'intervalDays'),
    repetitions: requireNumber(item.repetitions, 'repetitions'),
    nextReviewAt: requireString(item.nextReviewAt, 'nextReviewAt').slice(0, 10),
    lastReviewedAt: optionalString(item.lastReviewedAt),
    updatedAt: requireString(item.updatedAt, 'updatedAt'),
  };
}

function mapGamification(item: Record<string, unknown>): SyncGamificationWire {
  return {
    totalXp: requireNumber(item.totalXp, 'totalXp'),
    currentStreak: requireNumber(item.currentStreak, 'currentStreak'),
    longestStreak: requireNumber(item.longestStreak, 'longestStreak'),
    lastActivityDate: optionalString(item.lastActivityDate)?.slice(0, 10) ?? null,
    progressLevel: mapLevel(item.progressLevel),
    xpDelta: optionalNumber(item.xpDelta) ?? 0,
    updatedAt: requireString(item.updatedAt, 'updatedAt'),
  };
}

function mapChanges(value: unknown): SyncChanges {
  const body = (value ?? {}) as Record<string, unknown>;
  const progress = Array.isArray(body.progress) ? body.progress : [];
  const spacedRepetition = Array.isArray(body.spacedRepetition) ? body.spacedRepetition : [];
  const gamification =
    body.gamification && typeof body.gamification === 'object'
      ? mapGamification(body.gamification as Record<string, unknown>)
      : null;

  return {
    progress: progress.map((item) => mapProgress(item as Record<string, unknown>)),
    spacedRepetition: spacedRepetition.map((item) => mapSrs(item as Record<string, unknown>)),
    gamification,
  };
}

export function mapSyncPushResult(body: unknown): SyncPushResult {
  const row = (body ?? {}) as Record<string, unknown>;
  return {
    syncVersion: requireNumber(row.syncVersion, 'syncVersion'),
    lastSyncedAt: requireString(row.lastSyncedAt, 'lastSyncedAt'),
    idempotentReplay: row.idempotentReplay === true,
    serverChanges: mapChanges(row.serverChanges),
  };
}
