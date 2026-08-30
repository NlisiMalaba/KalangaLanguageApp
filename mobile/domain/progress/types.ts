import type { CalendarDate, EntityId } from '@/domain/entities';
import type { Level } from '@/domain/enums';

export type ProgressLessonSummary = {
  id: EntityId;
  level: Level;
  category: string;
};

export type ProgressCompletion = {
  lessonId: EntityId;
  completedAt: string | null;
  score: number | null;
};

export type ExerciseScoreSample = {
  lessonId: EntityId;
  score: number;
};

export type ProgressBucket = {
  level: Level | null;
  category: string | null;
  completedCount: number;
  totalCount: number;
  percentage: number;
};

export type WeakArea = {
  level: Level;
  category: string;
  averageScore: number;
  sampleSize: number;
};

export type LearnerProgressSnapshot = {
  totalXp: number;
  currentStreak: number;
  longestStreak: number;
  progressLevel: Level;
  lastActivityDate: CalendarDate | null;
};

export type GetProgressInput = {
  languageId: EntityId;
  userId: EntityId;
};

export type GetProgressResult = {
  userId: EntityId;
  totalXp: number;
  currentStreak: number;
  longestStreak: number;
  progressLevel: Level;
  lastActivityDate: CalendarDate | null;
  byLevel: ProgressBucket[];
  byCategory: ProgressBucket[];
  weakAreas: WeakArea[];
};
