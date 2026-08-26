import { NetworkRequiredError } from '@/domain/auth/errors';
import type { AuthUseCaseDeps } from '@/domain/auth/ports';
import { persistAuthenticatedSession } from '@/domain/auth/persistSession';
import type { AuthSession, RegisterInput } from '@/domain/auth/session';
import { requireDisplayName, requireEmail, requireLanguageId, requirePassword } from '@/domain/auth/validation';

export function createRegisterUseCase(deps: AuthUseCaseDeps) {
  return async function register(input: RegisterInput): Promise<AuthSession> {
    const languageId = requireLanguageId(input.languageId);
    const email = requireEmail(input.email);
    const password = requirePassword(input.password, { requireMinLength: true });
    const displayName = requireDisplayName(input.displayName);

    if (!(await deps.network.isOnline())) {
      throw new NetworkRequiredError('Registration');
    }

    await deps.authApi.register({ languageId, email, password, displayName });
    const session = await deps.authApi.login({ languageId, email, password });
    return persistAuthenticatedSession(deps, session, password);
  };
}
