import type { EntityId, LearnerGamification, LearnerProgress } from '@/domain/entities';
import { Level } from '@/domain/enums';
import {
  bucketsByCategory,
  bucketsByLevel,
  emptyGamificationSnapshot,
  identifyWeakAreas,
} from '@/domain/progress/summarizeProgress';
import type {
  ExerciseScoreSample,
  GetProgressInput,
  GetProgressResult,
  ProgressCompletion,
  ProgressLessonSummary,
} from '@/domain/progress/types';

export type GetProgressDeps = {
  listPublishedLessons: (languageId: EntityId) => Promise<ProgressLessonSummary[]>;
  listProgress: (languageId: EntityId, userId: EntityId) => Promise<LearnerProgress[]>;
  listExerciseScores: (languageId: EntityId, userId: EntityId) => Promise<ExerciseScoreSample[]>;
  getGamification: (languageId: EntityId, userId: EntityId) => Promise<LearnerGamification | null>;
};

function toCompletion(row: LearnerProgress): ProgressCompletion {
  return {
    lessonId: row.lessonId,
    completedAt: row.completedAt,
    score: row.score,
  };
}

function snapshotFrom(gamification: LearnerGamification | null) {
  if (!gamification) {
    return emptyGamificationSnapshot(Level.Beginner);
  }

  return {
    totalXp: gamification.totalXp,
    currentStreak: gamification.currentStreak,
    longestStreak: gamification.longestStreak,
    progressLevel: gamification.progressLevel,
    lastActivityDate: gamification.lastActivityDate,
  };
}

export function createGetProgressUseCase(deps: GetProgressDeps) {
  return async function getProgress(input: GetProgressInput): Promise<GetProgressResult> {
    const [published, progressRows, exerciseScores, gamification] = await Promise.all([
      deps.listPublishedLessons(input.languageId),
      deps.listProgress(input.languageId, input.userId),
      deps.listExerciseScores(input.languageId, input.userId),
      deps.getGamification(input.languageId, input.userId),
    ]);

    const completions = progressRows.map(toCompletion);
    const totals = snapshotFrom(gamification);

    return {
      userId: input.userId,
      totalXp: totals.totalXp,
      currentStreak: totals.currentStreak,
      longestStreak: totals.longestStreak,
      progressLevel: totals.progressLevel,
      lastActivityDate: totals.lastActivityDate,
      byLevel: bucketsByLevel(published, completions),
      byCategory: bucketsByCategory(published, completions),
      weakAreas: identifyWeakAreas(published, completions, exerciseScores),
    };
  };
}
