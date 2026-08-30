/** Mirrors sqlite last-write-wins: incoming replaces local when `updated_at` is greater or equal. */
export function incomingWinsLastWrite(
  localUpdatedAt: string | null | undefined,
  incomingUpdatedAt: string,
): boolean {
  return localUpdatedAt == null || incomingUpdatedAt >= localUpdatedAt;
}

export function takeLastWriteWins<T extends { updatedAt: string }>(local: T | null, incoming: T): T {
  return incomingWinsLastWrite(local?.updatedAt, incoming.updatedAt) ? incoming : (local as T);
}
