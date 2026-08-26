import type { NetworkStatus } from '@/domain/auth/ports';
import type { EntityId, Instant } from '@/domain/entities';
import type { Level } from '@/domain/enums';
import type { CatalogLessonSummary, LessonDetail } from './types';

export type CatalogFilter = {
  level?: Level | null;
  category?: string | null;
  skip: number;
  take: number;
};

export type CatalogProgressRow = {
  lessonId: EntityId;
  completedAt: Instant | null;
};

export type CatalogPackRow = {
  id: EntityId;
  lessonIds: readonly EntityId[];
};

export type CatalogDownloadRow = {
  contentPackId: EntityId;
  status: string;
};

export type LessonCatalogApi = {
  listPublished(languageId: EntityId, filter: CatalogFilter): Promise<CatalogLessonSummary[]>;
  getLesson(languageId: EntityId, lessonId: EntityId): Promise<LessonDetail>;
};

export type BrowseLessonCatalogDeps = {
  listLocalLessons: (languageId: EntityId, filter: CatalogFilter) => Promise<CatalogLessonSummary[]>;
  listProgress: (languageId: EntityId, userId: EntityId) => Promise<CatalogProgressRow[]>;
  listContentPacks: (languageId: EntityId) => Promise<CatalogPackRow[]>;
  listDownloadProgress: (languageId: EntityId) => Promise<CatalogDownloadRow[]>;
  catalogApi: LessonCatalogApi;
  network: NetworkStatus;
};

export type GetLessonDeps = {
  loadLocalLesson: (languageId: EntityId, lessonId: EntityId) => Promise<LessonDetail | null>;
  catalogApi: Pick<LessonCatalogApi, 'getLesson'>;
  network: NetworkStatus;
};
