import { LessonStatus, Level } from '@/domain/enums';
import { listPublishedLessons } from '@/lib/lessons';
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

describe('listPublishedLessons', () => {
  it('filters sqlite by tenant, published status, level, and category', async () => {
    const store = createRecordingStore();
    await listPublishedLessons('lang-1', store, { level: Level.Beginner, category: ' Everyday ' });

    expect(store.lastQuery?.sql).toContain('language_id = ?');
    expect(store.lastQuery?.sql).toContain('status = ?');
    expect(store.lastQuery?.sql).toContain('level = ?');
    expect(store.lastQuery?.sql).toContain('category = ?');
    expect(store.lastQuery?.params).toEqual(['lang-1', LessonStatus.Published, Level.Beginner, 'Everyday']);
  });
});
