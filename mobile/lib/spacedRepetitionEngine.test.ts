import { createSqliteSpacedRepetitionStore } from '@/lib/sqliteSpacedRepetitionStore';
import type { LocalStore, SqlValue } from '@/lib/localStore';

function createRecordingStore(): LocalStore & { lastQuery: { sql: string; params: SqlValue[] } | null } {
  const store: LocalStore & { lastQuery: { sql: string; params: SqlValue[] } | null } = {
    lastQuery: null,
    exec: async () => undefined,
    run: async () => undefined,
    getAll: async (sql, params = []) => {
      store.lastQuery = { sql, params };
      return [];
    },
    getFirst: async () => null,
    transaction: async (fn) => fn(store),
  };
  return store;
}

describe('createSqliteSpacedRepetitionStore', () => {
  it('loads the daily review from sqlite by tenant, user, and due date', async () => {
    const db = createRecordingStore();
    const store = createSqliteSpacedRepetitionStore(db);
    await store.listDue('lang-1', 'user-1', '2026-08-22');
    expect(db.lastQuery?.sql).toContain('next_review_at <= ?');
    expect(db.lastQuery?.params).toEqual(['lang-1', 'user-1', '2026-08-22']);
  });
});
