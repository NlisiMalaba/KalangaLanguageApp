import { RequestApiError } from '@/domain/requests/errors';
import type { CommunityRequest } from '@/domain/requests/types';
import { RequestStatus } from '@/domain/enums';
import { requireNumber, requireString, unwrapId, type IdWire } from '@/lib/catalog/wire';
import { CatalogApiError } from '@/domain/catalog/errors';

type RequestWire = {
  requestId?: IdWire;
  languageId?: IdWire;
  submitterId?: IdWire;
  title?: unknown;
  description?: unknown;
  upvoteCount?: unknown;
  status?: unknown;
  fulfilledByLessonId?: IdWire;
  createdAt?: unknown;
  updatedAt?: unknown;
};

function unwrap(value: IdWire, field: string): string {
  try {
    return unwrapId(value, field);
  } catch (error) {
    if (error instanceof CatalogApiError) {
      throw new RequestApiError(error.message.replace('Catalog response', 'Request response'));
    }

    throw error;
  }
}

function optionalId(value: IdWire): string | null {
  if (value == null) {
    return null;
  }

  if (typeof value === 'object' && (value.value == null || value.value === '')) {
    return null;
  }

  return unwrap(value, 'fulfilledByLessonId');
}

function mapStatus(value: unknown): RequestStatus {
  if (typeof value === 'string' && (Object.values(RequestStatus) as string[]).includes(value)) {
    return value as RequestStatus;
  }

  return RequestStatus.Open;
}

export function mapRequestDto(body: unknown): CommunityRequest {
  const row = (body ?? {}) as RequestWire;
  try {
    return {
      id: unwrap(row.requestId, 'requestId'),
      languageId: unwrap(row.languageId, 'languageId'),
      submitterId: unwrap(row.submitterId, 'submitterId'),
      title: requireString(row.title, 'title'),
      description: requireString(row.description, 'description'),
      upvoteCount: requireNumber(row.upvoteCount, 'upvoteCount'),
      status: mapStatus(row.status),
      fulfilledByLessonId: optionalId(row.fulfilledByLessonId),
      createdAt: requireString(row.createdAt, 'createdAt'),
      updatedAt: requireString(row.updatedAt, 'updatedAt'),
    };
  } catch (error) {
    if (error instanceof CatalogApiError) {
      throw new RequestApiError(error.message.replace('Catalog response', 'Request response'));
    }

    throw error;
  }
}

export function mapListRequestsResponse(body: unknown): CommunityRequest[] {
  const requests = (body as { requests?: unknown } | null)?.requests;
  if (!Array.isArray(requests)) {
    throw new RequestApiError('Request response is missing requests.');
  }

  return requests.map(mapRequestDto);
}

export function mapSubmitRequestResponse(body: unknown): CommunityRequest {
  const request = (body as { request?: unknown } | null)?.request;
  if (!request) {
    throw new RequestApiError('Request response is missing request.');
  }

  return mapRequestDto(request);
}

export function mapUpvoteRequestResponse(body: unknown): { request: CommunityRequest; applied: boolean } {
  const row = (body ?? {}) as { request?: unknown; applied?: unknown };
  if (!row.request) {
    throw new RequestApiError('Request response is missing request.');
  }

  return {
    request: mapRequestDto(row.request),
    applied: row.applied === true,
  };
}
