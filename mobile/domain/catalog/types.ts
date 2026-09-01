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

export type AudioRef = {
  id: EntityId;
  cdnUrl: string;
  fileFormat: string;
  speakerGender: string;
  dialectLabel: string | null;
  durationMs: number;
};

export type LessonVariationDetail = {
  id: EntityId;
  kalangaText: string;
  registerLabel: string;
  audio: AudioRef[];
};

export type LessonPhraseDetail = {
  id: EntityId;
  kalangaText: string;
  englishTranslation: string;
  sortOrder: number;
  variations: LessonVariationDetail[];
  audio: AudioRef[];
};

export type LessonExerciseDetail = {
  id: EntityId;
  exerciseType: string;
  promptData: string;
  correctAnswer: string;
  sortOrder: number;
};

export type LessonDetail = CatalogLessonSummary & {
  phrases: LessonPhraseDetail[];
  exercises: LessonExerciseDetail[];
};

export type BrowseLessonCatalogInput = {
  languageId: EntityId;
  userId: EntityId;
  level?: Level | null;
  category?: string | null;
  isScenario?: boolean | null;
  skip?: number;
  take?: number;
};
