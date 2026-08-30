import type { AuthUseCaseDeps } from '@/domain/auth/ports';
import { createLoginUseCase } from '@/domain/auth/loginUseCase';
import { createRegisterUseCase } from '@/domain/auth/registerUseCase';
import { createBcryptPasswordHasher } from '@/lib/auth/bcryptPasswordHasher';
import { createExpoNetworkStatus } from '@/lib/auth/expoNetworkStatus';
import { createHttpAuthApi } from '@/lib/auth/httpAuthApi';
import { createSecureCredentialStore } from '@/lib/auth/secureCredentialStore';

export function createAuthUseCases(overrides: Partial<AuthUseCaseDeps> = {}) {
  const deps: AuthUseCaseDeps = {
    authApi: createHttpAuthApi(),
    credentials: createSecureCredentialStore(),
    passwordHasher: createBcryptPasswordHasher(),
    network: createExpoNetworkStatus(),
    ...overrides,
  };

  return {
    register: createRegisterUseCase(deps),
    login: createLoginUseCase(deps),
  };
}
