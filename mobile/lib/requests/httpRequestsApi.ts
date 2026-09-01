import { AuthApiError } from '@/domain/auth/errors';
import { RequestApiError } from '@/domain/requests/errors';
import type { RequestsApi } from '@/domain/requests/types';
import {
  mapListRequestsResponse,
  mapSubmitRequestResponse,
  mapUpvoteRequestResponse,
} from '@/lib/requests/mapRequestResponse';
import { apiRequest } from '@/utils/api';

function wrap(error: unknown): never {
  if (error instanceof AuthApiError) {
    throw new RequestApiError(error.message);
  }

  throw error;
}

export function createHttpRequestsApi(): RequestsApi {
  return {
    async list(input) {
      try {
        const params = new URLSearchParams();
        params.set('skip', String(input.skip ?? 0));
        params.set('take', String(input.take ?? 50));
        const body = await apiRequest<unknown>(`/requests?${params.toString()}`, { method: 'GET' });
        return mapListRequestsResponse(body).filter((item) => item.languageId === input.languageId);
      } catch (error) {
        wrap(error);
      }
    },
    async submit(input) {
      try {
        const body = await apiRequest<unknown>('/requests', {
          method: 'POST',
          body: { title: input.title, description: input.description },
        });
        return mapSubmitRequestResponse(body);
      } catch (error) {
        wrap(error);
      }
    },
    async upvote(input) {
      try {
        const body = await apiRequest<unknown>(`/requests/${input.requestId}/upvote`, { method: 'POST' });
        return mapUpvoteRequestResponse(body);
      } catch (error) {
        wrap(error);
      }
    },
    async fulfill(input) {
      try {
        const body = await apiRequest<unknown>(`/requests/${input.requestId}/fulfill`, {
          method: 'POST',
          body: { lessonId: input.lessonId },
        });
        return mapSubmitRequestResponse(body);
      } catch (error) {
        wrap(error);
      }
    },
  };
}
