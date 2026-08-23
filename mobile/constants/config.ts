const DEFAULT_API_BASE_URL = 'https://localhost:7253';

/**
 * Public API origin for the mobile client. Override with EXPO_PUBLIC_API_BASE_URL.
 * Tokens stay in expo-secure-store; this value is not a secret.
 */
export function getApiBaseUrl(): string {
  const fromEnv = process.env.EXPO_PUBLIC_API_BASE_URL?.trim();
  if (fromEnv && fromEnv.length > 0) {
    return fromEnv.replace(/\/+$/, '');
  }

  return DEFAULT_API_BASE_URL;
}
