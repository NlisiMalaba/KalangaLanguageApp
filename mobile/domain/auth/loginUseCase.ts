import {
  AuthValidationError,
  DuplicateEmailError,
  InvalidCredentialsError,
  UserSuspendedError,
} from '@/domain/auth/errors';
import type { AuthUseCaseDeps } from '@/domain/auth/ports';
import { persistAuthenticatedSession } from '@/domain/auth/persistSession';
import { toAuthSession } from '@/domain/auth/ports';
import type { AuthSession, LoginInput } from '@/domain/auth/session';
import { requireEmail, requireLanguageId, requirePassword } from '@/domain/auth/validation';

function isRetriableOffline(error: unknown): boolean {
  return !(
    error instanceof InvalidCredentialsError ||
    error instanceof UserSuspendedError ||
    error instanceof DuplicateEmailError ||
    error instanceof AuthValidationError
  );
}

export function createLoginUseCase(deps: AuthUseCaseDeps) {
  async function loginOffline(languageId: string, email: string, password: string): Promise<AuthSession> {
    const stored = await deps.credentials.load();
    if (stored === null || stored.user.languageId !== languageId || stored.user.email !== email) {
      throw new InvalidCredentialsError();
    }

    const matches = await deps.passwordHasher.verify(password, stored.passwordHash);
    if (!matches) {
      throw new InvalidCredentialsError();
    }

    return toAuthSession(stored.user, stored.tokens);
  }

  return async function login(input: LoginInput): Promise<AuthSession> {
    const languageId = requireLanguageId(input.languageId);
    const email = requireEmail(input.email);
    const password = requirePassword(input.password, { requireMinLength: false });

    if (await deps.network.isOnline()) {
      try {
        const session = await deps.authApi.login({ languageId, email, password });
        return persistAuthenticatedSession(deps, session, password);
      } catch (error) {
        if (!isRetriableOffline(error)) {
          throw error;
        }

        try {
          return await loginOffline(languageId, email, password);
        } catch {
          throw error;
        }
      }
    }

    return loginOffline(languageId, email, password);
  };
}
