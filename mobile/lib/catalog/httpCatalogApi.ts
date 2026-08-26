import { Level } from '@/domain/enums';
import { CatalogApiError } from '@/domain/catalog/errors';
import type { CatalogFilter, LessonCatalogApi } from '@/domain/catalog/ports';
import type { CatalogLessonSummary } from '@/domain/catalog/types';
import type { EntityId } from '@/domain/entities';
import { apiRequest } from '@/utils/api';

type IdWire = string | { value?: unknown } | null | undefined;

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

const LEVEL_BY_NUMBER: Level[] = [Level.Beginner, Level.Intermediate, Level.Advanced];

function unwrapId(value: IdWire, field: string): EntityId {
  if (typeof value === 'string' && value.length > 0) {
    return value;
  }

  if (value && typeof value === 'object' && typeof value.value === 'string' && value.value.length > 0) {
    return value.value;
  }

  throw new CatalogApiError(`Catalog response is missing ${field}.`);
}

function mapLevel(value: unknown): Level {
  if (typeof value === 'string' && (Object.values(Level) as string[]).includes(value)) {
    return value as Level;
  }

  if (typeof value === 'number' && LEVEL_BY_NUMBER[value]) {
    return LEVEL_BY_NUMBER[value];
  }

  throw new CatalogApiError('Catalog response has an invalid level.');
}

function mapItem(item: CatalogItemWire): CatalogLessonSummary {
  if (typeof item.title !== 'string' || item.title.length === 0) {
    throw new CatalogApiError('Catalog response is missing a title.');
  }

  if (typeof item.category !== 'string' || item.category.length === 0) {
    throw new CatalogApiError('Catalog response is missing a category.');
  }

  if (typeof item.xpReward !== 'number') {
    throw new CatalogApiError('Catalog response is missing xpReward.');
  }

  if (typeof item.updatedAt !== 'string' || item.updatedAt.length === 0) {
    throw new CatalogApiError('Catalog response is missing updatedAt.');
  }

  return {
    id: unwrapId(item.lessonId, 'lessonId'),
    languageId: unwrapId(item.languageId, 'languageId'),
    title: item.title,
    level: mapLevel(item.level),
    category: item.category,
    isScenario: item.isScenario === true,
    scenarioContext: typeof item.scenarioContext === 'string' ? item.scenarioContext : null,
    xpReward: item.xpReward,
    updatedAt: item.updatedAt,
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

      const body = await apiRequest<unknown>(`/lessons?${params.toString()}`, { method: 'GET' });
      const items = mapBrowseLessonCatalogResponse(body);
      return items.filter((item) => item.languageId === languageId);
    },
  };
}
