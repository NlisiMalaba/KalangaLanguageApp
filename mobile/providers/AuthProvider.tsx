import { PropsWithChildren, useCallback, useEffect, useMemo, useState } from 'react';

import { getLanguageId } from '@/constants/config';
import { AuthContext } from '@/ctx/AuthContext';
import type { AuthUser } from '@/domain/auth/session';
import { createAuthUseCases } from '@/lib/auth/createAuthUseCases';
import { createSecureCredentialStore } from '@/lib/auth/secureCredentialStore';
import { loadOnboardingProfile, type OnboardingProfile } from '@/lib/onboarding';
import { apiRequest } from '@/utils/api';
import { bindTokenSessionListeners, setTokenSession } from '@/utils/tokenSession';

const credentials = createSecureCredentialStore();
const authUseCases = createAuthUseCases({ credentials });

function applySession(user: AuthUser, accessToken: string, refreshToken: string): void {
  setTokenSession({
    languageId: user.languageId,
    accessToken,
    refreshToken,
  });
}

export default function AuthProvider({ children }: PropsWithChildren) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [profile, setProfile] = useState<OnboardingProfile | null>(null);
  const [loading, setLoading] = useState(true);

  const refreshProfile = useCallback(async () => {
    if (!user) {
      setProfile(null);
      return;
    }

    setProfile(await loadOnboardingProfile(user.id));
  }, [user]);

  const signOutLocal = useCallback(async () => {
    setTokenSession(null);
    setUser(null);
    setProfile(null);
    await credentials.clear();
  }, []);

  useEffect(() => {
    bindTokenSessionListeners({
      persistTokens: (tokens) => credentials.updateTokens(tokens),
      onSessionInvalid: () => {
        void signOutLocal();
      },
    });

    return () => bindTokenSessionListeners(null);
  }, [signOutLocal]);

  useEffect(() => {
    const restore = async () => {
      setLoading(true);
      try {
        const stored = await credentials.load();
        if (!stored) {
          setUser(null);
          setProfile(null);
          return;
        }

        applySession(stored.user, stored.tokens.accessToken, stored.tokens.refreshToken);
        setUser(stored.user);
        setProfile(await loadOnboardingProfile(stored.user.id));
      } finally {
        setLoading(false);
      }
    };

    void restore();
  }, []);

  const signIn = useCallback(async (email: string, password: string) => {
    const session = await authUseCases.login({
      languageId: getLanguageId(),
      email,
      password,
    });
    const nextUser: AuthUser = {
      id: session.id,
      languageId: session.languageId,
      email: session.email,
      displayName: session.displayName,
      role: session.role,
    };
    applySession(nextUser, session.accessToken, session.refreshToken);
    setUser(nextUser);
    setProfile(await loadOnboardingProfile(nextUser.id));
  }, []);

  const signUp = useCallback(async (email: string, password: string, displayName: string) => {
    const session = await authUseCases.register({
      languageId: getLanguageId(),
      email,
      password,
      displayName,
    });
    const nextUser: AuthUser = {
      id: session.id,
      languageId: session.languageId,
      email: session.email,
      displayName: session.displayName,
      role: session.role,
    };
    applySession(nextUser, session.accessToken, session.refreshToken);
    setUser(nextUser);
    setProfile(await loadOnboardingProfile(nextUser.id));
  }, []);

  const signOut = useCallback(async () => {
    const stored = await credentials.load();
    try {
      if (stored) {
        await apiRequest('/auth/logout', {
          method: 'POST',
          body: { languageId: stored.user.languageId, refreshToken: stored.tokens.refreshToken },
          skipAuth: true,
          skipRefresh: true,
        });
      }
    } catch {
      // Local sign-out still proceeds if the API is unreachable.
    }

    await signOutLocal();
  }, [signOutLocal]);

  const value = useMemo(
    () => ({
      user,
      profile,
      loading,
      signIn,
      signUp,
      signOut,
      refreshProfile,
    }),
    [user, profile, loading, signIn, signUp, signOut, refreshProfile],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
