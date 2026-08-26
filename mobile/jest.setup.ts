jest.mock('expo-sqlite', () => ({
  openDatabaseAsync: jest.fn(async () => {
    throw new Error('expo-sqlite is not available in unit tests; inject a LocalStore.');
  }),
}));
