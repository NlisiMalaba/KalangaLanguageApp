jest.mock('expo-sqlite', () => ({
  openDatabaseAsync: jest.fn(async () => {
    throw new Error('expo-sqlite is not available in unit tests; inject a LocalStore.');
  }),
}));

jest.mock('expo-secure-store', () => ({
  WHEN_UNLOCKED: 'WHEN_UNLOCKED',
  setItemAsync: jest.fn(),
  getItemAsync: jest.fn(),
  deleteItemAsync: jest.fn(),
}));

jest.mock('@react-native-async-storage/async-storage', () =>
  require('@react-native-async-storage/async-storage/jest/async-storage-mock'),
);

jest.mock('expo-network', () => ({
  getNetworkStateAsync: jest.fn(async () => ({ isConnected: true, isInternetReachable: true })),
}));

jest.mock('expo-file-system/legacy', () => ({
  documentDirectory: 'file:///docs/',
  getInfoAsync: jest.fn(async () => ({ exists: false })),
}));

jest.mock('expo-av', () => ({
  Audio: {
    Sound: jest.fn().mockImplementation(() => ({
      loadAsync: jest.fn(),
      playAsync: jest.fn(),
      stopAsync: jest.fn(),
      unloadAsync: jest.fn(),
      setPositionAsync: jest.fn(),
      setRateAsync: jest.fn(),
    })),
    setAudioModeAsync: jest.fn(),
  },
}));

jest.mock('expo-speech', () => ({
  speak: jest.fn(),
  stop: jest.fn(),
}));

jest.mock('sonner-native', () => ({
  toast: {
    error: jest.fn(),
    success: jest.fn(),
    info: jest.fn(),
  },
  Toaster: () => null,
}));

process.env.EXPO_PUBLIC_LANGUAGE_ID ??= '11111111-1111-7111-8111-111111111111';
