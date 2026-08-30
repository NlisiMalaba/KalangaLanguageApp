function newId(prefix: string): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `${prefix}-${Date.now()}`;
}

export function newProgressId(): string {
  return newId('progress');
}

export function newGamificationId(): string {
  return newId('xp');
}

export function newSyncId(): string {
  return newId('sync');
}

export function newExerciseResultId(): string {
  return newId('exr');
}

export function newSrsId(): string {
  return newId('srs');
}
