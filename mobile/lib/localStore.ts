export type SqlValue = string | number | null;

export type LocalStore = {
  exec(sql: string): Promise<void>;
  run(sql: string, params?: SqlValue[]): Promise<void>;
  getAll<T>(sql: string, params?: SqlValue[]): Promise<T[]>;
  getFirst<T>(sql: string, params?: SqlValue[]): Promise<T | null>;
  transaction<T>(fn: (store: LocalStore) => Promise<T>): Promise<T>;
};

export function requireLanguageId(languageId: string): string {
  const id = languageId.trim();
  if (id.length === 0) {
    throw new Error('language_id is required on every local store operation.');
  }

  return id;
}

export function requireTenantMatch(languageId: string, entityLanguageId: string): string {
  const tenant = requireLanguageId(languageId);
  if (entityLanguageId !== tenant) {
    throw new Error('Entity language_id does not match the store tenant.');
  }

  return tenant;
}
