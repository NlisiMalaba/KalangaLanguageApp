import type { AuthUseCaseDeps } from '@/domain/auth/ports';
import { toAuthSession } from '@/domain/auth/ports';
import type { AuthSession } from '@/domain/auth/session';

export async function persistAuthenticatedSession(
  deps: Pick<AuthUseCaseDeps, 'credentials' | 'passwordHasher'>,
  session: AuthSession,
  password: string,
): Promise<AuthSession> {
  const user = {
    id: session.id,
    languageId: session.languageId,
    email: session.email,
    displayName: session.displayName,
    role: session.role,
  };
  const tokens = {
    accessToken: session.accessToken,
    refreshToken: session.refreshToken,
    accessTokenExpiresAt: session.accessTokenExpiresAt,
    refreshTokenExpiresAt: session.refreshTokenExpiresAt,
  };

  const passwordHash = await deps.passwordHasher.hash(password);
  await deps.credentials.save({ user, tokens, passwordHash });
  return toAuthSession(user, tokens);
}
