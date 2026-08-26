import { createContext, useContext } from 'react';

import type { AuthUser } from '@/domain/auth/session';
import type { OnboardingProfile } from '@/lib/onboarding';

export type AuthContextValue = {
  user: AuthUser | null;
  profile: OnboardingProfile | null;
  loading: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (email: string, password: string, displayName: string) => Promise<void>;
  signOut: () => Promise<void>;
  refreshProfile: () => Promise<void>;
};

export const AuthContext = createContext<AuthContextValue>({
  user: null,
  profile: null,
  loading: true,
  signIn: async () => undefined,
  signUp: async () => undefined,
  signOut: async () => undefined,
  refreshProfile: async () => undefined,
});

export function useAuth(): AuthContextValue {
  return useContext(AuthContext);
}
