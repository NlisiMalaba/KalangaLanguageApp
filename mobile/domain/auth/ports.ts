import type { EntityId } from '@/domain/entities';
import type { Role } from '@/domain/enums';
import type { AuthSession, AuthTokens, AuthUser, StoredAuthRecord } from './session';

export type PasswordHasher = {
  hash(password: string): Promise<string>;
  verify(password: string, passwordHash: string): Promise<boolean>;
};

export type CredentialStore = {
  save(record: StoredAuthRecord): Promise<void>;
  load(): Promise<StoredAuthRecord | null>;
  updateTokens(tokens: AuthTokens): Promise<void>;
  clear(): Promise<void>;
};

export type NetworkStatus = {
  isOnline(): Promise<boolean>;
};

export type RegisterApiResult = {
  userId: EntityId;
  languageId: EntityId;
  email: string;
  displayName: string;
  role: Role;
};

export type LoginApiResult = AuthSession;

export type AuthApi = {
  register(input: {
    languageId: EntityId;
    email: string;
    password: string;
    displayName: string;
  }): Promise<RegisterApiResult>;
  login(input: {
    languageId: EntityId;
    email: string;
    password: string;
  }): Promise<LoginApiResult>;
};

export type AuthUseCaseDeps = {
  authApi: AuthApi;
  credentials: CredentialStore;
  passwordHasher: PasswordHasher;
  network: NetworkStatus;
};

export function toAuthSession(user: AuthUser, tokens: AuthTokens): AuthSession {
  return { ...user, ...tokens };
}
