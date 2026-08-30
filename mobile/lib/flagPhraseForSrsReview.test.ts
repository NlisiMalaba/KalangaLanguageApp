import { DEFAULT_SRS_INTERVAL_DAYS } from '@/constants/exercises';
import { createSqlitePhraseReviewFlagger } from '@/lib/flagPhraseForSrsReview';
import type { LocalStore, SqlValue } from '@/lib/localStore';
import type { SpacedRepetitionRow } from '@/lib/mappers';

function createRecordingStore(): LocalStore & {
  runs: { sql: string; params: SqlValue[] }[];
  first: SpacedRepetitionRow | null;
} {
  const store: LocalStore & { runs: { sql: string; params: SqlValue[] }[]; first: SpacedRepetitionRow | null } = {
    runs: [],
    first: null,
    exec: async () => undefined,
    run: async (sql, params = []) => {
      store.runs.push({ sql, params });
    },
    getAll: async () => [],
    getFirst: async <T>() => store.first as T | null,
    transaction: async (fn) => fn(store),
  };
  return store;
}

describe('createSqlitePhraseReviewFlagger', () => {
  it('writes a due-today SRS row for the missed phrase', async () => {
    const store = createRecordingStore();
    const flag = createSqlitePhraseReviewFlagger(store, () => new Date('2026-08-30T12:00:00.000Z'));
    await flag({ languageId: 'lang-1', userId: 'user-1', phraseId: 'phrase-1' });

    const insert = store.runs.find((run) => run.sql.includes('INSERT INTO spaced_repetition_records'));
    expect(insert?.params).toEqual(
      expect.arrayContaining(['lang-1', 'user-1', 'phrase-1', DEFAULT_SRS_INTERVAL_DAYS, '2026-08-30']),
    );
  });
});
