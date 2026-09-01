import type { EntityId } from '@/domain/entities';
import type { Level, LessonStatus } from '@/domain/enums';

export type ReviewQueueItem = {
  lessonId: EntityId;
  title: string;
  level: Level;
  category: string;
};

export type ReviewDecision = {
  lessonId: EntityId;
  status: LessonStatus;
};

export type ReviewApi = {
  listQueue: (languageId: EntityId) => Promise<ReviewQueueItem[]>;
  approve: (languageId: EntityId, lessonId: EntityId) => Promise<ReviewDecision>;
};
