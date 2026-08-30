import AsyncStorage from '@react-native-async-storage/async-storage';

import { Level } from '@/domain/enums';
import type { EntityId } from '@/domain/entities';

export type OnboardingProfile = {
  onboardingCompleted: boolean;
  startingLevel: Level | null;
  motivation: string | null;
  interests: string[];
};

const emptyProfile: OnboardingProfile = {
  onboardingCompleted: false,
  startingLevel: null,
  motivation: null,
  interests: [],
};

function storageKey(userId: EntityId): string {
  return `kalanga.onboarding.${userId}`;
}

export async function loadOnboardingProfile(userId: EntityId): Promise<OnboardingProfile> {
  const raw = await AsyncStorage.getItem(storageKey(userId));
  if (!raw) {
    return emptyProfile;
  }

  try {
    return { ...emptyProfile, ...(JSON.parse(raw) as Partial<OnboardingProfile>) };
  } catch {
    return emptyProfile;
  }
}

export async function saveOnboardingProfile(
  userId: EntityId,
  profile: OnboardingProfile,
): Promise<void> {
  await AsyncStorage.setItem(storageKey(userId), JSON.stringify(profile));
}

export async function clearOnboardingProfile(userId: EntityId): Promise<void> {
  await AsyncStorage.removeItem(storageKey(userId));
}
