import type { AuthTokens } from '@/domain/auth/session';
import type { EntityId } from '@/domain/entities';

export type TokenSession = {
  languageId: EntityId;
  accessToken: string;
  refreshToken: string;
};

type TokenSessionListeners = {
  persistTokens: (tokens: AuthTokens) => Promise<void>;
  onSessionInvalid: () => void;
};

let session: TokenSession | null = null;
let listeners: TokenSessionListeners | null = null;
let refreshInFlight: Promise<boolean> | null = null;

export function bindTokenSessionListeners(next: TokenSessionListeners | null): void {
  listeners = next;
}

export function setTokenSession(next: TokenSession | null): void {
  session = next;
}

export function getTokenSession(): TokenSession | null {
  return session;
}

export function resetTokenSessionForTests(): void {
  session = null;
  listeners = null;
  refreshInFlight = null;
}

export async function persistRefreshedTokens(tokens: AuthTokens): Promise<void> {
  if (session) {
    session = {
      ...session,
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
    };
  }

  await listeners?.persistTokens(tokens);
}

export function notifySessionInvalid(): void {
  session = null;
  listeners?.onSessionInvalid();
}

export function runExclusiveRefresh(work: () => Promise<boolean>): Promise<boolean> {
  if (refreshInFlight) {
    return refreshInFlight;
  }

  refreshInFlight = work().finally(() => {
    refreshInFlight = null;
  });

  return refreshInFlight;
}
