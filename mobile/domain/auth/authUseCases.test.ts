import { Role } from '@/domain/enums';
import { createLoginUseCase } from '@/domain/auth/loginUseCase';
import { createRegisterUseCase } from '@/domain/auth/registerUseCase';
import {
  InvalidCredentialsError,
  NetworkRequiredError,
  UserSuspendedError,
} from '@/domain/auth/errors';
import type { AuthApi, AuthUseCaseDeps, CredentialStore, PasswordHasher } from '@/domain/auth/ports';
import type { AuthSession, StoredAuthRecord } from '@/domain/auth/session';

const languageId = '11111111-1111-7111-8111-111111111111';

function sessionFixture(overrides: Partial<AuthSession> = {}): AuthSession {
  return {
    id: 'user-1',
    languageId,
    email: 'learner@example.test',
    displayName: 'Learner',
    role: Role.Learner,
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    accessTokenExpiresAt: '2026-08-24T12:00:00.000Z',
    refreshTokenExpiresAt: '2026-09-23T12:00:00.000Z',
    ...overrides,
  };
}

function createMemoryStore(initial: StoredAuthRecord | null = null): CredentialStore & { records: StoredAuthRecord[] } {
  const records: StoredAuthRecord[] = initial ? [initial] : [];
  return {
    records,
    save: async (record) => {
      records[0] = record;
    },
    load: async () => records[0] ?? null,
    clear: async () => {
      records.length = 0;
    },
  };
}

const hasher: PasswordHasher = {
  hash: async (password) => `bcrypt:${password}`,
  verify: async (password, passwordHash) => passwordHash === `bcrypt:${password}`,
};

function createDeps(overrides: Partial<AuthUseCaseDeps> & { authApi: AuthApi }): AuthUseCaseDeps {
  return {
    credentials: createMemoryStore(),
    passwordHasher: hasher,
    network: { isOnline: async () => true },
    ...overrides,
  };
}

describe('RegisterUseCase', () => {
  it('registers, logs in for JWTs, and stores a bcrypt hash without the raw password', async () => {
    const api: AuthApi = {
      register: jest.fn(async () => ({
        userId: 'user-1',
        languageId,
        email: 'learner@example.test',
        displayName: 'Learner',
        role: Role.Learner,
      })),
      login: jest.fn(async () => sessionFixture()),
    };
    const credentials = createMemoryStore();
    const register = createRegisterUseCase(createDeps({ authApi: api, credentials }));

    const result = await register({
      languageId,
      email: 'Learner@Example.test',
      password: 'password1',
      displayName: 'Learner',
    });

    expect(api.register).toHaveBeenCalledWith(
      expect.objectContaining({ email: 'learner@example.test', password: 'password1' }),
    );
    expect(api.login).toHaveBeenCalled();
    expect(result.accessToken).toBe('access-token');
    expect(result).not.toHaveProperty('passwordHash');
    expect(credentials.records[0]?.passwordHash).toBe('bcrypt:password1');
    expect(credentials.records[0]).not.toHaveProperty('password');
  });

  it('does not register while offline', async () => {
    const api: AuthApi = {
      register: jest.fn(),
      login: jest.fn(),
    };
    const register = createRegisterUseCase(
      createDeps({ authApi: api, network: { isOnline: async () => false } }),
    );

    await expect(
      register({
        languageId,
        email: 'learner@example.test',
        password: 'password1',
        displayName: 'Learner',
      }),
    ).rejects.toBeInstanceOf(NetworkRequiredError);
    expect(api.register).not.toHaveBeenCalled();
  });
});

describe('LoginUseCase', () => {
  it('stores tokens and a password hash on online login', async () => {
    const api: AuthApi = {
      register: jest.fn(),
      login: jest.fn(async () => sessionFixture()),
    };
    const credentials = createMemoryStore();
    const login = createLoginUseCase(createDeps({ authApi: api, credentials }));

    const result = await login({ languageId, email: 'learner@example.test', password: 'password1' });

    expect(result.refreshToken).toBe('refresh-token');
    expect(credentials.records[0]?.passwordHash).toBe('bcrypt:password1');
  });

  it('authenticates offline against the cached bcrypt hash and restores the last JWT', async () => {
    const storedUser = sessionFixture();
    const credentials = createMemoryStore({
      user: {
        id: storedUser.id,
        languageId: storedUser.languageId,
        email: storedUser.email,
        displayName: storedUser.displayName,
        role: storedUser.role,
      },
      tokens: {
        accessToken: 'cached-access',
        refreshToken: 'cached-refresh',
        accessTokenExpiresAt: storedUser.accessTokenExpiresAt,
        refreshTokenExpiresAt: storedUser.refreshTokenExpiresAt,
      },
      passwordHash: 'bcrypt:password1',
    });
    const api: AuthApi = {
      register: jest.fn(),
      login: jest.fn(async () => {
        throw new Error('should not call API offline');
      }),
    };
    const login = createLoginUseCase(
      createDeps({
        authApi: api,
        credentials,
        network: { isOnline: async () => false },
      }),
    );

    const ok = await login({ languageId, email: 'learner@example.test', password: 'password1' });
    expect(ok.accessToken).toBe('cached-access');
    expect(api.login).not.toHaveBeenCalled();

    await expect(
      login({ languageId, email: 'learner@example.test', password: 'wrong-pass' }),
    ).rejects.toBeInstanceOf(InvalidCredentialsError);
  });

  it('does not fall back to cached credentials when the server rejects the password', async () => {
    const storedUser = sessionFixture();
    const credentials = createMemoryStore({
      user: {
        id: storedUser.id,
        languageId: storedUser.languageId,
        email: storedUser.email,
        displayName: storedUser.displayName,
        role: storedUser.role,
      },
      tokens: {
        accessToken: 'cached-access',
        refreshToken: 'cached-refresh',
        accessTokenExpiresAt: storedUser.accessTokenExpiresAt,
        refreshTokenExpiresAt: storedUser.refreshTokenExpiresAt,
      },
      passwordHash: 'bcrypt:password1',
    });
    const api: AuthApi = {
      register: jest.fn(),
      login: jest.fn(async () => {
        throw new InvalidCredentialsError();
      }),
    };
    const login = createLoginUseCase(createDeps({ authApi: api, credentials }));

    await expect(
      login({ languageId, email: 'learner@example.test', password: 'password1' }),
    ).rejects.toBeInstanceOf(InvalidCredentialsError);
  });

  it('does not authenticate a suspended account returned by the API', async () => {
    const api: AuthApi = {
      register: jest.fn(),
      login: jest.fn(async () => {
        throw new UserSuspendedError();
      }),
    };
    const login = createLoginUseCase(createDeps({ authApi: api }));

    await expect(
      login({ languageId, email: 'learner@example.test', password: 'password1' }),
    ).rejects.toBeInstanceOf(UserSuspendedError);
  });
});
