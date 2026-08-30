import { CatalogApiError } from '@/domain/catalog/errors';
import type { EntityId } from '@/domain/entities';
import { Level } from '@/domain/enums';

export type IdWire = string | { value?: unknown } | null | undefined;

export const LEVEL_BY_NUMBER: Level[] = [Level.Beginner, Level.Intermediate, Level.Advanced];

export function unwrapId(value: IdWire, field: string): EntityId {
  if (typeof value === 'string' && value.length > 0) {
    return value;
  }

  if (value && typeof value === 'object' && typeof value.value === 'string' && value.value.length > 0) {
    return value.value;
  }

  throw new CatalogApiError(`Catalog response is missing ${field}.`);
}

export function mapLevel(value: unknown): Level {
  if (typeof value === 'string' && (Object.values(Level) as string[]).includes(value)) {
    return value as Level;
  }

  if (typeof value === 'number' && LEVEL_BY_NUMBER[value]) {
    return LEVEL_BY_NUMBER[value];
  }

  throw new CatalogApiError('Catalog response has an invalid level.');
}

export function requireString(value: unknown, field: string): string {
  if (typeof value !== 'string' || value.length === 0) {
    throw new CatalogApiError(`Catalog response is missing ${field}.`);
  }

  return value;
}

export function requireNumber(value: unknown, field: string): number {
  if (typeof value !== 'number') {
    throw new CatalogApiError(`Catalog response is missing ${field}.`);
  }

  return value;
}
