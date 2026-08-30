import * as Linking from 'expo-linking';

import type { AppSettingsOpener } from '@/domain/pronunciation/types';

export function createExpoAppSettingsOpener(): AppSettingsOpener {
  return {
    openSettings: () => Linking.openSettings(),
  };
}
