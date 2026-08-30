import { MAX_LESSON_SCORE } from '@/constants/progress';
import type { EntityId, LearnerGamification, LearnerProgress } from '@/domain/entities';
import { InvalidLessonCompletionError } from '@/domain/progress/errors';
import {
  advanceLevelIfThresholdCrossed,
  awardXp,
  createLearnerGamification,
  recordActivity,
} from '@/domain/progress/gamification';
import { newSyncId } from '@/domain/progress/ids';
import {
  completeLessonProgress,
  isLessonCompleted,
  startLessonProgress,
} from '@/domain/progress/learnerProgress';
import { utcCalendarDate } from '@/domain/srs/calendarDate';

export type CompleteLessonInput = {
  languageId: EntityId;
  userId: EntityId;
  lessonId: EntityId;
  score: number;
  xpReward: number;
  now?: Date;
};

export type CompleteLessonResult = {
  lessonId: EntityId;
  completedAt: string;
  score: number;
  xpAwarded: number;
  totalXp: number;
  xpGranted: boolean;
  currentStreak: number;
};

export type EnqueueSyncItem = {
  id: EntityId;
  languageId: EntityId;
  userId: EntityId;
  clientOperationId: string;
  entityType: 'progress' | 'gamification' | 'spaced_repetition';
  payload: string;
  createdAt: string;
  updatedAt: string;
};

export type CompleteLessonDeps = {
  getProgress: (
    languageId: EntityId,
    userId: EntityId,
    lessonId: EntityId,
  ) => Promise<LearnerProgress | null>;
  upsertProgress: (progress: LearnerProgress) => Promise<void>;
  getGamification: (languageId: EntityId, userId: EntityId) => Promise<LearnerGamification | null>;
  upsertGamification: (gamification: LearnerGamification) => Promise<void>;
  enqueueSync: (item: EnqueueSyncItem) => Promise<void>;
};

function requireScore(score: number): number {
  if (!Number.isInteger(score) || score < 0 || score > MAX_LESSON_SCORE) {
    throw new InvalidLessonCompletionError(`Score must be between 0 and ${MAX_LESSON_SCORE}.`);
  }

  return score;
}

export function createCompleteLessonUseCase(deps: CompleteLessonDeps) {
  return async function completeLesson(input: CompleteLessonInput): Promise<CompleteLessonResult> {
    const score = requireScore(input.score);
    const xpReward = Math.max(0, input.xpReward);
    const now = input.now ?? new Date();
    const existing = await deps.getProgress(input.languageId, input.userId, input.lessonId);

    let recorded: LearnerProgress;
    let xpGranted = false;

    if (!existing) {
      recorded = completeLessonProgress(
        startLessonProgress({
          languageId: input.languageId,
          userId: input.userId,
          lessonId: input.lessonId,
          now,
        }),
        score,
        xpReward,
        now,
      );
      xpGranted = xpReward > 0;
    } else if (isLessonCompleted(existing)) {
      recorded = completeLessonProgress(existing, score, existing.xpAwarded, now);
    } else {
      recorded = completeLessonProgress(existing, score, xpReward, now);
      xpGranted = xpReward > 0;
    }

    await deps.upsertProgress(recorded);

    let gamification =
      (await deps.getGamification(input.languageId, input.userId)) ??
      createLearnerGamification({ languageId: input.languageId, userId: input.userId, now });

    if (xpGranted) {
      gamification = awardXp(gamification, xpReward, now);
    }

    gamification = recordActivity(gamification, utcCalendarDate(now), now);
    gamification = advanceLevelIfThresholdCrossed(gamification, now);
    await deps.upsertGamification(gamification);

    const instant = now.toISOString();
    await deps.enqueueSync({
      id: newSyncId(),
      languageId: input.languageId,
      userId: input.userId,
      clientOperationId: `progress:${input.userId}:${input.lessonId}:${recorded.updatedAt}`,
      entityType: 'progress',
      payload: JSON.stringify(recorded),
      createdAt: instant,
      updatedAt: instant,
    });
    await deps.enqueueSync({
      id: newSyncId(),
      languageId: input.languageId,
      userId: input.userId,
      clientOperationId: `gamification:${input.userId}:${gamification.updatedAt}`,
      entityType: 'gamification',
      payload: JSON.stringify(gamification),
      createdAt: instant,
      updatedAt: instant,
    });

    return {
      lessonId: recorded.lessonId,
      completedAt: recorded.completedAt ?? instant,
      score: recorded.score ?? score,
      xpAwarded: recorded.xpAwarded,
      totalXp: gamification.totalXp,
      xpGranted,
      currentStreak: gamification.currentStreak,
    };
  };
}
