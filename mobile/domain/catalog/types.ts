import type { EntityId, Instant } from '@/domain/entities';
import type { Level } from '@/domain/enums';

export const LessonDownloadStatus = {
  NotDownloaded: 'not_downloaded',
  Partial: 'partial',
  Downloaded: 'downloaded',
} as const;
export type LessonDownloadStatus =
  (typeof LessonDownloadStatus)[keyof typeof LessonDownloadStatus];

export type CatalogLessonSummary = {
  id: EntityId;
  languageId: EntityId;
  title: string;
  level: Level;
  category: string;
  isScenario: boolean;
  scenarioContext: string | null;
  xpReward: number;
  updatedAt: Instant;
};

export type CatalogLessonItem = CatalogLessonSummary & {
  isCompleted: boolean;
  downloadStatus: LessonDownloadStatus;
};

export type BrowseLessonCatalogInput = {
  languageId: EntityId;
  userId: EntityId;
  level?: Level | null;
  category?: string | null;
  skip?: number;
  take?: number;
};
