import { AuthApiError } from '@/domain/auth/errors';
import { CatalogApiError, LessonNotFoundError } from '@/domain/catalog/errors';
import type { CatalogFilter, LessonCatalogApi } from '@/domain/catalog/ports';
import type { CatalogLessonSummary } from '@/domain/catalog/types';
import type { EntityId } from '@/domain/entities';
import { mapGetLessonResponse } from '@/lib/catalog/mapGetLessonResponse';
import { mapLevel, requireNumber, requireString, unwrapId, type IdWire } from '@/lib/catalog/wire';
import { apiRequest } from '@/utils/api';

type CatalogItemWire = {
  lessonId?: IdWire;
  languageId?: IdWire;
  title?: unknown;
  level?: unknown;
  category?: unknown;
  isScenario?: unknown;
  scenarioContext?: unknown;
  xpReward?: unknown;
  updatedAt?: unknown;
};

type CatalogResponseWire = {
  lessons?: CatalogItemWire[];
};

function mapItem(item: CatalogItemWire): CatalogLessonSummary {
  return {
    id: unwrapId(item.lessonId, 'lessonId'),
    languageId: unwrapId(item.languageId, 'languageId'),
    title: requireString(item.title, 'title'),
    level: mapLevel(item.level),
    category: requireString(item.category, 'category'),
    isScenario: item.isScenario === true,
    scenarioContext: typeof item.scenarioContext === 'string' ? item.scenarioContext : null,
    xpReward: requireNumber(item.xpReward, 'xpReward'),
    updatedAt: requireString(item.updatedAt, 'updatedAt'),
  };
}

export function mapBrowseLessonCatalogResponse(body: unknown): CatalogLessonSummary[] {
  const lessons = (body as CatalogResponseWire | null)?.lessons;
  if (!Array.isArray(lessons)) {
    throw new CatalogApiError('Catalog response is missing lessons.');
  }

  return lessons.map(mapItem);
}

export function createHttpCatalogApi(): LessonCatalogApi {
  return {
    async listPublished(languageId: EntityId, filter: CatalogFilter): Promise<CatalogLessonSummary[]> {
      const params = new URLSearchParams();
      params.set('skip', String(filter.skip));
      params.set('take', String(filter.take));
      if (filter.level) {
        params.set('level', filter.level);
      }
      if (filter.category) {
        params.set('category', filter.category);
      }
      if (filter.isScenario === true) {
        params.set('isScenario', 'true');
      } else if (filter.isScenario === false) {
        params.set('isScenario', 'false');
      }

      const body = await apiRequest<unknown>(`/lessons?${params.toString()}`, { method: 'GET' });
      const items = mapBrowseLessonCatalogResponse(body);
      return items.filter((item) => item.languageId === languageId);
    },
    async getLesson(languageId: EntityId, lessonId: EntityId) {
      try {
        const body = await apiRequest<unknown>(`/lessons/${lessonId}`, { method: 'GET' });
        const detail = mapGetLessonResponse(body);
        if (detail.languageId !== languageId) {
          throw new LessonNotFoundError(lessonId);
        }

        return detail;
      } catch (error) {
        if (error instanceof AuthApiError && error.status === 404) {
          throw new LessonNotFoundError(lessonId);
        }

        throw error;
      }
    },
  };
}
