import { createLoginUseCase } from '@/domain/auth/loginUseCase';
import { createRegisterUseCase } from '@/domain/auth/registerUseCase';
import { createBcryptPasswordHasher } from '@/lib/auth/bcryptPasswordHasher';
import { createExpoNetworkStatus } from '@/lib/auth/expoNetworkStatus';
import { createHttpAuthApi } from '@/lib/auth/httpAuthApi';
import { createSecureCredentialStore } from '@/lib/auth/secureCredentialStore';

export function createAuthUseCases() {
  const deps = {
    authApi: createHttpAuthApi(),
    credentials: createSecureCredentialStore(),
    passwordHasher: createBcryptPasswordHasher(),
    network: createExpoNetworkStatus(),
  };

  return {
    register: createRegisterUseCase(deps),
    login: createLoginUseCase(deps),
  };
}
