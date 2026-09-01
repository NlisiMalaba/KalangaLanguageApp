import { AuthApiError } from '@/domain/auth/errors';
import { CatalogApiError } from '@/domain/catalog/errors';
import { LessonStatus } from '@/domain/enums';
import { ReviewApiError } from '@/domain/review/errors';
import type { ReviewApi, ReviewDecision, ReviewQueueItem } from '@/domain/review/types';
import { mapLevel, unwrapId, type IdWire } from '@/lib/catalog/wire';
import { apiRequest } from '@/utils/api';

function wrap(error: unknown): never {
  if (error instanceof AuthApiError || error instanceof CatalogApiError) {
    throw new ReviewApiError(error.message);
  }

  throw error;
}

function mapItem(row: {
  lessonId?: IdWire;
  title?: unknown;
  level?: unknown;
  category?: unknown;
}): ReviewQueueItem {
  try {
    return {
      lessonId: unwrapId(row.lessonId, 'lessonId'),
      title: typeof row.title === 'string' ? row.title : '',
      level: mapLevel(row.level),
      category: typeof row.category === 'string' ? row.category : '',
    };
  } catch (error) {
    wrap(error);
  }
}

function mapStatus(value: unknown): LessonStatus {
  if (typeof value === 'string' && (Object.values(LessonStatus) as string[]).includes(value)) {
    return value as LessonStatus;
  }

  return LessonStatus.Published;
}

export function createHttpReviewApi(): ReviewApi {
  return {
    async listQueue(_languageId) {
      try {
        const body = await apiRequest<{ lessons?: unknown[] }>('/review/queue', { method: 'GET' });
        const lessons = Array.isArray(body.lessons) ? body.lessons : [];
        return lessons
          .map((item) => mapItem((item ?? {}) as Parameters<typeof mapItem>[0]))
          .filter((item) => item.title.length > 0);
      } catch (error) {
        wrap(error);
      }
    },
    async approve(_languageId, lessonId) {
      try {
        const body = await apiRequest<{ lessonId?: IdWire; status?: unknown }>(
          `/review/${lessonId}/approve`,
          { method: 'POST' },
        );
        const decision: ReviewDecision = {
          lessonId: unwrapId(body.lessonId, 'lessonId'),
          status: mapStatus(body.status),
        };
        return decision;
      } catch (error) {
        wrap(error);
      }
    },
  };
}
