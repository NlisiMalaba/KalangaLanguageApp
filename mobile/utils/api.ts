import { CORRELATION_HEADER, HTTP_TIMEOUT_MS } from '@/constants/auth';
import { getApiBaseUrl } from '@/constants/config';
import {
  AuthApiError,
  DuplicateEmailError,
  InvalidCredentialsError,
  SessionExpiredError,
  UserSuspendedError,
} from '@/domain/auth/errors';
import type { AuthTokens } from '@/domain/auth/session';
import {
  getTokenSession,
  notifySessionInvalid,
  persistRefreshedTokens,
  runExclusiveRefresh,
} from '@/utils/tokenSession';

export type ApiRequestOptions = {
  method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  timeoutMs?: number;
  skipAuth?: boolean;
  skipRefresh?: boolean;
  mapAuthFailures?: boolean;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
};

type RefreshResponse = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
};

function createCorrelationId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `kalanga-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function mapFailure(status: number, detail: string, mapAuthFailures: boolean): never {
  if (mapAuthFailures) {
    if (status === 401) {
      throw new InvalidCredentialsError();
    }

    if (status === 403) {
      throw new UserSuspendedError();
    }

    if (status === 409) {
      throw new DuplicateEmailError('');
    }
  }

  if (status === 401) {
    throw new SessionExpiredError();
  }

  throw new AuthApiError(status, detail);
}

async function parseBody(response: Response): Promise<unknown> {
  if (response.status === 204) {
    return undefined;
  }

  const text = await response.text();
  return text.length === 0 ? null : (JSON.parse(text) as unknown);
}

async function send(path: string, options: ApiRequestOptions, accessToken: string | null): Promise<Response> {
  const controller = new AbortController();
  const timeoutMs = options.timeoutMs ?? HTTP_TIMEOUT_MS;
  const timeout = setTimeout(() => controller.abort(), timeoutMs);
  const headers: Record<string, string> = {
    Accept: 'application/json',
    'Content-Type': 'application/json',
    [CORRELATION_HEADER]: createCorrelationId(),
  };

  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  try {
    return await fetch(`${getApiBaseUrl()}${path}`, {
      method: options.method,
      signal: controller.signal,
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    });
  } finally {
    clearTimeout(timeout);
  }
}

async function refreshTokens(): Promise<boolean> {
  return runExclusiveRefresh(async () => {
    const current = getTokenSession();
    if (current === null) {
      return false;
    }

    const response = await send(
      '/auth/refresh',
      {
        method: 'POST',
        body: { languageId: current.languageId, refreshToken: current.refreshToken },
        skipAuth: true,
        skipRefresh: true,
      },
      null,
    );

    if (!response.ok) {
      notifySessionInvalid();
      return false;
    }

    const data = (await parseBody(response)) as RefreshResponse;
    const tokens: AuthTokens = {
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      accessTokenExpiresAt: data.accessTokenExpiresAt,
      refreshTokenExpiresAt: data.refreshTokenExpiresAt,
    };
    await persistRefreshedTokens(tokens);
    return true;
  });
}

export async function apiRequest<T>(path: string, options: ApiRequestOptions): Promise<T> {
  const mapAuthFailures = options.mapAuthFailures === true;
  const token = options.skipAuth ? null : (getTokenSession()?.accessToken ?? null);
  let response = await send(path, options, token);

  if (response.status === 401 && !options.skipRefresh && !options.skipAuth) {
    const refreshed = await refreshTokens();
    if (refreshed) {
      const retryToken = getTokenSession()?.accessToken ?? null;
      response = await send(path, options, retryToken);
    } else {
      throw new SessionExpiredError();
    }
  }

  const data = await parseBody(response);

  if (!response.ok) {
    const problem = (data ?? {}) as ProblemDetails;
    const detail = problem.detail ?? problem.title ?? `Request failed (${response.status}).`;
    mapFailure(response.status, detail, mapAuthFailures);
  }

  return data as T;
}
