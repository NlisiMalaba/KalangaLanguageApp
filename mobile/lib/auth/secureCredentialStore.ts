import * as SecureStore from 'expo-secure-store';

import type { CredentialStore } from '@/domain/auth/ports';
import type { StoredAuthRecord } from '@/domain/auth/session';

const USER_KEY = 'kalanga.session.user';
const ACCESS_TOKEN_KEY = 'kalanga.session.accessToken';
const REFRESH_TOKEN_KEY = 'kalanga.session.refreshToken';
const ACCESS_EXPIRES_KEY = 'kalanga.session.accessExpires';
const REFRESH_EXPIRES_KEY = 'kalanga.session.refreshExpires';
const PASSWORD_HASH_KEY = 'kalanga.session.passwordHash';

const secureOptions: SecureStore.SecureStoreOptions = {
  keychainAccessible: SecureStore.WHEN_UNLOCKED,
};

export function createSecureCredentialStore(): CredentialStore {
  return {
    async save(record: StoredAuthRecord): Promise<void> {
      await Promise.all([
        SecureStore.setItemAsync(USER_KEY, JSON.stringify(record.user), secureOptions),
        SecureStore.setItemAsync(ACCESS_TOKEN_KEY, record.tokens.accessToken, secureOptions),
        SecureStore.setItemAsync(REFRESH_TOKEN_KEY, record.tokens.refreshToken, secureOptions),
        SecureStore.setItemAsync(ACCESS_EXPIRES_KEY, record.tokens.accessTokenExpiresAt, secureOptions),
        SecureStore.setItemAsync(REFRESH_EXPIRES_KEY, record.tokens.refreshTokenExpiresAt, secureOptions),
        SecureStore.setItemAsync(PASSWORD_HASH_KEY, record.passwordHash, secureOptions),
      ]);
    },

    async load(): Promise<StoredAuthRecord | null> {
      const [userJson, accessToken, refreshToken, accessExpires, refreshExpires, passwordHash] =
        await Promise.all([
          SecureStore.getItemAsync(USER_KEY, secureOptions),
          SecureStore.getItemAsync(ACCESS_TOKEN_KEY, secureOptions),
          SecureStore.getItemAsync(REFRESH_TOKEN_KEY, secureOptions),
          SecureStore.getItemAsync(ACCESS_EXPIRES_KEY, secureOptions),
          SecureStore.getItemAsync(REFRESH_EXPIRES_KEY, secureOptions),
          SecureStore.getItemAsync(PASSWORD_HASH_KEY, secureOptions),
        ]);

      if (
        userJson === null ||
        accessToken === null ||
        refreshToken === null ||
        accessExpires === null ||
        refreshExpires === null ||
        passwordHash === null
      ) {
        return null;
      }

      return {
        user: JSON.parse(userJson) as StoredAuthRecord['user'],
        tokens: {
          accessToken,
          refreshToken,
          accessTokenExpiresAt: accessExpires,
          refreshTokenExpiresAt: refreshExpires,
        },
        passwordHash,
      };
    },

    async clear(): Promise<void> {
      await Promise.all([
        SecureStore.deleteItemAsync(USER_KEY, secureOptions),
        SecureStore.deleteItemAsync(ACCESS_TOKEN_KEY, secureOptions),
        SecureStore.deleteItemAsync(REFRESH_TOKEN_KEY, secureOptions),
        SecureStore.deleteItemAsync(ACCESS_EXPIRES_KEY, secureOptions),
        SecureStore.deleteItemAsync(REFRESH_EXPIRES_KEY, secureOptions),
        SecureStore.deleteItemAsync(PASSWORD_HASH_KEY, secureOptions),
      ]);
    },
  };
}
