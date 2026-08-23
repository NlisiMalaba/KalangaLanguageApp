import { getApiBaseUrl } from '@/constants/config';

describe('getApiBaseUrl', () => {
  const original = process.env.EXPO_PUBLIC_API_BASE_URL;

  afterEach(() => {
    process.env.EXPO_PUBLIC_API_BASE_URL = original;
  });

  it('uses the HTTPS development origin when no env override is set', () => {
    delete process.env.EXPO_PUBLIC_API_BASE_URL;
    expect(getApiBaseUrl()).toBe('https://localhost:7253');
  });

  it('strips a trailing slash from EXPO_PUBLIC_API_BASE_URL', () => {
    process.env.EXPO_PUBLIC_API_BASE_URL = 'https://api.example.test/';
    expect(getApiBaseUrl()).toBe('https://api.example.test');
  });
});
