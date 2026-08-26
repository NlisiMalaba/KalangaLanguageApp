import type { LearnerProgress } from '@/domain/entities';
import { upsertLessonProgress } from '@/lib/lessonProgress';
import type { LocalStore, SqlValue } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';

function createRecordingStore(): LocalStore & { statements: { sql: string; params: SqlValue[] }[] } {
  const statements: { sql: string; params: SqlValue[] }[] = [];
  const store: LocalStore & { statements: typeof statements } = {
    statements,
    exec: async () => undefined,
    run: async (sql, params = []) => {
      statements.push({ sql, params });
    },
    getAll: async () => [],
    getFirst: async () => null,
    transaction: async (fn) => fn(store),
  };
  return store;
}

describe('local store tenant guard', () => {
  it('rejects a missing language_id', () => {
    expect(() => requireLanguageId('  ')).toThrow(/language_id is required/);
  });

  it('rejects a cross-tenant upsert', () => {
    expect(() => requireTenantMatch('lang-a', 'lang-b')).toThrow(/does not match/);
  });
});

describe('lessonProgress helpers', () => {
  const progress: LearnerProgress = {
    id: 'progress-1',
    languageId: 'lang-1',
    userId: 'user-1',
    lessonId: 'lesson-1',
    completedAt: '2026-08-23T12:00:00.000Z',
    score: 90,
    xpAwarded: 10,
    updatedAt: '2026-08-23T12:00:00.000Z',
  };

  it('upserts with language_id and last-write-wins on updated_at', async () => {
    const store = createRecordingStore();
    await upsertLessonProgress('lang-1', progress, store);

    expect(store.statements).toHaveLength(1);
    expect(store.statements[0]?.sql).toContain('INSERT INTO learner_progress');
    expect(store.statements[0]?.sql).toContain('excluded.updated_at >= learner_progress.updated_at');
    expect(store.statements[0]?.params).toContain('lang-1');
    expect(store.statements[0]?.params).toContain(progress.userId);
    expect(store.statements[0]?.params).toContain(progress.lessonId);
  });

  it('does not write when the entity belongs to another language', async () => {
    const store = createRecordingStore();
    await expect(upsertLessonProgress('lang-other', progress, store)).rejects.toThrow(
      /does not match/,
    );
    expect(store.statements).toHaveLength(0);
  });
});
