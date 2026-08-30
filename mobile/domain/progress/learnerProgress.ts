import type { EntityId, Instant, LearnerProgress } from '@/domain/entities';
import { newProgressId } from '@/domain/progress/ids';

export function startLessonProgress(input: {
  languageId: EntityId;
  userId: EntityId;
  lessonId: EntityId;
  now: Date;
}): LearnerProgress {
  const instant = input.now.toISOString();
  return {
    id: newProgressId(),
    languageId: input.languageId,
    userId: input.userId,
    lessonId: input.lessonId,
    completedAt: null,
    score: null,
    xpAwarded: 0,
    updatedAt: instant,
  };
}

export function completeLessonProgress(
  progress: LearnerProgress,
  score: number,
  xpAwarded: number,
  now: Date,
): LearnerProgress {
  const instant = now.toISOString();
  return {
    ...progress,
    completedAt: instant,
    score,
    xpAwarded,
    updatedAt: instant,
  };
}

export function isLessonCompleted(progress: LearnerProgress): boolean {
  return progress.completedAt != null;
}
