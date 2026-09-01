import bcrypt from 'bcryptjs';
import { getRandomBytes } from 'expo-crypto';

import { BCRYPT_WORK_FACTOR } from '@/constants/auth';
import type { PasswordHasher } from '@/domain/auth/ports';

let randomFallbackConfigured = false;

function ensureBcryptRandomFallback(): void {
  if (randomFallbackConfigured) {
    return;
  }

  // bcryptjs 3 calls genSalt then hashes; on React Native, Web Crypto / Node crypto
  // are often missing, so genSalt fails and hash is called with an undefined salt.
  bcrypt.setRandomFallback((length) => Array.from(getRandomBytes(length)));
  randomFallbackConfigured = true;
}

export function createBcryptPasswordHasher(workFactor: number = BCRYPT_WORK_FACTOR): PasswordHasher {
  ensureBcryptRandomFallback();

  return {
    hash: async (password) => {
      const salt = await bcrypt.genSalt(workFactor);
      return bcrypt.hash(password, salt);
    },
    verify: async (password, passwordHash) => {
      if (password.length === 0 || passwordHash.length === 0) {
        return false;
      }

      try {
        return await bcrypt.compare(password, passwordHash);
      } catch {
        return false;
      }
    },
  };
}
