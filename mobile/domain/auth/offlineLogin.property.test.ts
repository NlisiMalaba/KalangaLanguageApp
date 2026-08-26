import * as fc from 'fast-check';

import { Role } from '@/domain/enums';
import { InvalidCredentialsError } from '@/domain/auth/errors';
import { createLoginUseCase } from '@/domain/auth/loginUseCase';
import type { AuthApi, CredentialStore, PasswordHasher } from '@/domain/auth/ports';
import type { StoredAuthRecord } from '@/domain/auth/session';

const languageId = '11111111-1111-7111-8111-111111111111';
const email = 'learner@example.test';

const hasher: PasswordHasher = {
  hash: async (password) => `bcrypt:${password}`,
  verify: async (password, passwordHash) => passwordHash === `bcrypt:${password}`,
};

function createMemoryStore(initial: StoredAuthRecord): CredentialStore {
  let record: StoredAuthRecord | null = initial;
  return {
    save: async (next) => {
      record = next;
    },
    load: async () => record,
    clear: async () => {
      record = null;
    },
  };
}

function cachedRecord(password: string): StoredAuthRecord {
  return {
    user: {
      id: 'user-1',
      languageId,
      email,
      displayName: 'Learner',
      role: Role.Learner,
    },
    tokens: {
      accessToken: 'cached-access',
      refreshToken: 'cached-refresh',
      accessTokenExpiresAt: '2026-08-24T12:00:00.000Z',
      refreshTokenExpiresAt: '2026-09-23T12:00:00.000Z',
    },
    passwordHash: `bcrypt:${password}`,
  };
}

const passwordArb = fc.string({ minLength: 1, maxLength: 128 });

describe('offline login with cached credentials', () => {
  // Feature: kalanga-language-app, Property 4: Offline Login with Cached Credentials
  it('succeeds with the cached password and fails with any different password', async () => {
    await fc.assert(
      fc.asyncProperty(
        fc.tuple(passwordArb, passwordArb).filter(([password, wrongPassword]) => password !== wrongPassword),
        async ([password, wrongPassword]) => {
          const api: AuthApi = {
            register: async () => {
              throw new Error('API must not be called while offline.');
            },
            login: async () => {
              throw new Error('API must not be called while offline.');
            },
          };
          const login = createLoginUseCase({
            authApi: api,
            credentials: createMemoryStore(cachedRecord(password)),
            passwordHasher: hasher,
            network: { isOnline: async () => false },
          });

          const session = await login({ languageId, email, password });
          expect(session.accessToken).toBe('cached-access');
          expect(session.refreshToken).toBe('cached-refresh');

          await expect(login({ languageId, email, password: wrongPassword })).rejects.toBeInstanceOf(
            InvalidCredentialsError,
          );
        },
      ),
      { numRuns: 100 },
    );
  });
});
