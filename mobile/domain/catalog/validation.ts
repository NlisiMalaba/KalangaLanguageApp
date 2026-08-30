import { CatalogValidationError } from '@/domain/catalog/errors';
import type { EntityId } from '@/domain/entities';

export function requireCatalogId(value: string, field: string): EntityId {
  const id = value.trim();
  if (id.length === 0) {
    throw new CatalogValidationError(`${field} is required.`);
  }

  return id;
}
