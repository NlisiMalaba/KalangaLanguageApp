import { CORRELATION_HEADER, HTTP_TIMEOUT_MS } from '@/constants/auth';
import { getApiBaseUrl } from '@/constants/config';
import { AuthApiError, DuplicateEmailError, InvalidCredentialsError, UserSuspendedError } from '@/domain/auth/errors';

export type ApiRequestOptions = {
  method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  timeoutMs?: number;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
};

function createCorrelationId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `kalanga-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function mapAuthFailure(status: number, detail: string): never {
  if (status === 401) {
    throw new InvalidCredentialsError();
  }

  if (status === 403) {
    throw new UserSuspendedError();
  }

  if (status === 409) {
    throw new DuplicateEmailError('');
  }

  throw new AuthApiError(status, detail);
}

export async function apiRequest<T>(path: string, options: ApiRequestOptions): Promise<T> {
  const controller = new AbortController();
  const timeoutMs = options.timeoutMs ?? HTTP_TIMEOUT_MS;
  const timeout = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch(`${getApiBaseUrl()}${path}`, {
      method: options.method,
      signal: controller.signal,
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        [CORRELATION_HEADER]: createCorrelationId(),
      },
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    });

    if (response.status === 204) {
      return undefined as T;
    }

    const text = await response.text();
    const data = text.length === 0 ? null : (JSON.parse(text) as unknown);

    if (!response.ok) {
      const problem = (data ?? {}) as ProblemDetails;
      const detail = problem.detail ?? problem.title ?? `Request failed (${response.status}).`;
      mapAuthFailure(response.status, detail);
    }

    return data as T;
  } catch (error) {
    if (error instanceof InvalidCredentialsError || error instanceof UserSuspendedError || error instanceof DuplicateEmailError || error instanceof AuthApiError) {
      throw error;
    }

    throw error;
  } finally {
    clearTimeout(timeout);
  }
}
