import { getApiBaseUrl, getLanguageId, rewriteLoopbackForDevice } from '@/constants/config';

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

describe('rewriteLoopbackForDevice', () => {
  it('rewrites loopback HTTPS to the Expo LAN host and HTTP 5077', () => {
    expect(rewriteLoopbackForDevice('https://localhost:7253', '192.168.1.20')).toBe(
      'http://192.168.1.20:5077',
    );
  });

  it('leaves a non-loopback origin unchanged', () => {
    expect(rewriteLoopbackForDevice('https://api.example.test', '192.168.1.20')).toBe(
      'https://api.example.test',
    );
  });
});

describe('getLanguageId', () => {
  const original = process.env.EXPO_PUBLIC_LANGUAGE_ID;

  afterEach(() => {
    process.env.EXPO_PUBLIC_LANGUAGE_ID = original;
  });

  it('reads EXPO_PUBLIC_LANGUAGE_ID', () => {
    process.env.EXPO_PUBLIC_LANGUAGE_ID = 'lang-tenant';
    expect(getLanguageId()).toBe('lang-tenant');
  });

  it('replaces the all-zero placeholder with the seeded Kalanga tenant', () => {
    process.env.EXPO_PUBLIC_LANGUAGE_ID = '00000000-0000-0000-0000-000000000001';
    expect(getLanguageId()).toBe('a1b2c3d4-e5f6-4780-8bcd-ef1234567890');
  });
});
