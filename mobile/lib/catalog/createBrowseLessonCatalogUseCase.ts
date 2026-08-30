import type { CatalogLessonSummary } from '@/domain/catalog/types';
import type { BrowseLessonCatalogDeps, GetLessonDeps } from '@/domain/catalog/ports';
import { createBrowseLessonCatalogUseCase } from '@/domain/catalog/browseLessonCatalogUseCase';
import { createGetLessonUseCase } from '@/domain/catalog/getLessonUseCase';
import { createExpoNetworkStatus } from '@/lib/auth/expoNetworkStatus';
import { createHttpCatalogApi } from '@/lib/catalog/httpCatalogApi';
import { loadLocalLessonDetail } from '@/lib/catalog/loadLocalLessonDetail';
import { listContentPacks } from '@/lib/contentPacks';
import { listDownloadProgressForLanguage } from '@/lib/downloadProgress';
import { listLessonProgressForUser } from '@/lib/lessonProgress';
import { listPublishedLessons } from '@/lib/lessons';
import type { LocalStore } from '@/lib/localStore';
import type { Lesson } from '@/domain/entities';

function toCatalogSummary(lesson: Lesson): CatalogLessonSummary {
  return {
    id: lesson.id,
    languageId: lesson.languageId,
    title: lesson.title,
    level: lesson.level,
    category: lesson.category,
    isScenario: lesson.isScenario,
    scenarioContext: lesson.scenarioContext,
    xpReward: lesson.xpReward,
    updatedAt: lesson.updatedAt,
  };
}

export function createDefaultBrowseLessonCatalogUseCase(
  overrides: Partial<BrowseLessonCatalogDeps> & { store?: LocalStore } = {},
) {
  const store = overrides.store;
  const deps: BrowseLessonCatalogDeps = {
    listLocalLessons: async (languageId, filter) => {
      const lessons = await listPublishedLessons(languageId, store, {
        level: filter.level,
        category: filter.category,
      });
      return lessons.map(toCatalogSummary);
    },
    listProgress: async (languageId, userId) => {
      const rows = await listLessonProgressForUser(languageId, userId, store);
      return rows.map((row) => ({ lessonId: row.lessonId, completedAt: row.completedAt }));
    },
    listContentPacks: async (languageId) => {
      const packs = await listContentPacks(languageId, store);
      return packs.map((pack) => ({ id: pack.id, lessonIds: pack.lessonIds }));
    },
    listDownloadProgress: async (languageId) => {
      const rows = await listDownloadProgressForLanguage(languageId, store);
      return rows.map((row) => ({ contentPackId: row.contentPackId, status: row.status }));
    },
    catalogApi: createHttpCatalogApi(),
    network: createExpoNetworkStatus(),
    ...overrides,
  };

  return createBrowseLessonCatalogUseCase(deps);
}

export function createDefaultGetLessonUseCase(
  overrides: Partial<GetLessonDeps> & { store?: LocalStore } = {},
) {
  const store = overrides.store;
  const deps: GetLessonDeps = {
    loadLocalLesson: (languageId, lessonId) => loadLocalLessonDetail(languageId, lessonId, store),
    catalogApi: createHttpCatalogApi(),
    network: createExpoNetworkStatus(),
    ...overrides,
  };

  return createGetLessonUseCase(deps);
}
