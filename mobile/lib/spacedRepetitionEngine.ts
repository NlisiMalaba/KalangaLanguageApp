import { createSpacedRepetitionEngine } from '@/domain/srs/spacedRepetitionEngine';
import type { SpacedRepetitionEngine } from '@/domain/srs/spacedRepetitionEngine';
import type { SpacedRepetitionStore } from '@/domain/srs/types';
import { createSqliteSpacedRepetitionStore } from '@/lib/sqliteSpacedRepetitionStore';
import type { LocalStore } from '@/lib/localStore';

export type { SpacedRepetitionEngine };

export function createDefaultSpacedRepetitionEngine(
  store?: LocalStore,
  overrides: Partial<SpacedRepetitionStore> = {},
): SpacedRepetitionEngine {
  return createSpacedRepetitionEngine({
    ...createSqliteSpacedRepetitionStore(store),
    ...overrides,
  });
}
