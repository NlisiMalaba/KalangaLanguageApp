import bcrypt from 'bcryptjs';

import { BCRYPT_WORK_FACTOR } from '@/constants/auth';
import type { PasswordHasher } from '@/domain/auth/ports';

export function createBcryptPasswordHasher(workFactor: number = BCRYPT_WORK_FACTOR): PasswordHasher {
  return {
    hash: (password) => bcrypt.hash(password, workFactor),
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
