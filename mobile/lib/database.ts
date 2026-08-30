import * as SQLite from 'expo-sqlite';

import { SCHEMA_SQL, SCHEMA_VERSION } from '@/lib/schema';
import type { LocalStore, SqlValue } from '@/lib/localStore';

type ExpoDatabase = Awaited<ReturnType<typeof SQLite.openDatabaseAsync>>;

function wrapExpoDatabase(db: ExpoDatabase): LocalStore {
  const store: LocalStore = {
    exec: (sql) => db.execAsync(sql),
    run: async (sql, params = []) => {
      await db.runAsync(sql, params);
    },
    getAll: <T>(sql: string, params: SqlValue[] = []) => db.getAllAsync<T>(sql, params),
    getFirst: async <T>(sql: string, params: SqlValue[] = []) => {
      const row = await db.getFirstAsync<T>(sql, params);
      return row ?? null;
    },
    transaction: async (fn) => {
      let result: Awaited<ReturnType<typeof fn>> | undefined;
      await db.withTransactionAsync(async () => {
        result = await fn(store);
      });
      return result as Awaited<ReturnType<typeof fn>>;
    },
  };

  return store;
}

async function applySchema(store: LocalStore): Promise<void> {
  await store.exec(SCHEMA_SQL);
  await store.run(
    `INSERT OR IGNORE INTO schema_migrations (version, applied_at) VALUES (?, ?)`,
    [SCHEMA_VERSION, new Date().toISOString()],
  );
}

let defaultStore: Promise<LocalStore> | null = null;

export function setLocalStoreForTests(store: LocalStore | null): void {
  defaultStore = store ? Promise.resolve(store) : null;
}

export async function getLocalStore(): Promise<LocalStore> {
  if (!defaultStore) {
    defaultStore = (async () => {
      const db = await SQLite.openDatabaseAsync('kalanga.db');
      const store = wrapExpoDatabase(db);
      await applySchema(store);
      return store;
    })();
  }

  return defaultStore;
}

export async function withStore<T>(
  store: LocalStore | undefined,
  fn: (resolved: LocalStore) => Promise<T>,
): Promise<T> {
  return fn(store ?? (await getLocalStore()));
}
