import type { EntityId } from '@/domain/entities';
import type { Role } from '@/domain/enums';

export type AuthUser = {
  id: EntityId;
  languageId: EntityId;
  email: string;
  displayName: string;
  role: Role;
};

export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
};

export type AuthSession = AuthUser & AuthTokens;

export type StoredAuthRecord = {
  user: AuthUser;
  tokens: AuthTokens;
  passwordHash: string;
};

export type RegisterInput = {
  languageId: EntityId;
  email: string;
  password: string;
  displayName: string;
};

export type LoginInput = {
  languageId: EntityId;
  email: string;
  password: string;
};
