import Constants from 'expo-constants';

const DEFAULT_API_BASE_URL = 'https://localhost:7253';
const DEV_HTTP_PORT = '5077';
const DEV_HTTPS_PORT = '7253';

/** Matches `WellKnownLanguages.KalangaId` seeded by the API in Development. */
export const KALANGA_LANGUAGE_ID = 'a1b2c3d4-e5f6-4780-8bcd-ef1234567890';

const PLACEHOLDER_LANGUAGE_ID = '00000000-0000-0000-0000-000000000001';

/**
 * Public API origin for the mobile client. Override with EXPO_PUBLIC_API_BASE_URL.
 * Tokens stay in expo-secure-store; this value is not a secret.
 *
 * Loopback HTTPS (the ASP.NET dev cert) is not usable from a physical device, so in
 * development we rewrite localhost to the Expo LAN host and HTTP :5077.
 */
export function getApiBaseUrl(): string {
  const fromEnv = process.env.EXPO_PUBLIC_API_BASE_URL?.trim();
  const origin = (fromEnv && fromEnv.length > 0 ? fromEnv : DEFAULT_API_BASE_URL).replace(/\/+$/, '');
  return rewriteLoopbackForDevice(origin, expoLanHost());
}

export function getLanguageId(): string {
  const fromEnv = process.env.EXPO_PUBLIC_LANGUAGE_ID?.trim();
  if (!fromEnv || fromEnv === PLACEHOLDER_LANGUAGE_ID) {
    return KALANGA_LANGUAGE_ID;
  }

  return fromEnv;
}

export function rewriteLoopbackForDevice(origin: string, lanHost: string | undefined): string {
  if (typeof __DEV__ === 'undefined' || !__DEV__) {
    return origin;
  }

  let url: URL;
  try {
    url = new URL(origin);
  } catch {
    return origin;
  }

  const loopback = url.hostname === 'localhost' || url.hostname === '127.0.0.1';
  if (!loopback || !lanHost || lanHost === 'localhost' || lanHost === '127.0.0.1') {
    return origin;
  }

  url.protocol = 'http:';
  url.hostname = lanHost;
  if (url.port === DEV_HTTPS_PORT || url.port === '') {
    url.port = DEV_HTTP_PORT;
  }

  return url.origin;
}

function expoLanHost(): string | undefined {
  const hostUri = Constants.expoConfig?.hostUri;
  if (!hostUri) {
    return undefined;
  }

  const host = hostUri.split(':')[0]?.trim();
  if (!host || host === 'localhost' || host === '127.0.0.1') {
    return undefined;
  }

  return host;
}
