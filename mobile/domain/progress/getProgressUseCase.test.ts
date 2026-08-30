import { WEAK_AREA_SCORE_THRESHOLD } from '@/constants/progress';
import { Level } from '@/domain/enums';
import { createGetProgressUseCase } from '@/domain/progress/getProgressUseCase';
import type { GetProgressDeps } from '@/domain/progress/getProgressUseCase';
import { identifyWeakAreas, roundToOneDecimal } from '@/domain/progress/summarizeProgress';
import type { LearnerGamification, LearnerProgress } from '@/domain/entities';
import type { ProgressLessonSummary } from '@/domain/progress/types';

function lesson(id: string, level: Level, category: string): ProgressLessonSummary {
  return { id, level, category };
}

function progress(
  lessonId: string,
  score: number,
  completedAt = '2026-08-22T12:00:00.000Z',
): LearnerProgress {
  return {
    id: `p-${lessonId}`,
    languageId: 'lang-1',
    userId: 'user-1',
    lessonId,
    completedAt,
    score,
    xpAwarded: 10,
    updatedAt: completedAt,
  };
}

describe('getProgressUseCase', () => {
  it('computes completion percentages and weak areas from published lessons', async () => {
    const published: ProgressLessonSummary[] = [
      lesson('a', Level.Beginner, 'Everyday'),
      lesson('b', Level.Beginner, 'Everyday'),
      lesson('c', Level.Intermediate, 'Travel'),
    ];
    const rows: LearnerProgress[] = [progress('a', 40)];
    const gamification: LearnerGamification = {
      id: 'g-1',
      languageId: 'lang-1',
      userId: 'user-1',
      totalXp: 25,
      currentStreak: 3,
      longestStreak: 5,
      lastActivityDate: '2026-08-22',
      progressLevel: Level.Beginner,
      updatedAt: '2026-08-22T12:00:00.000Z',
    };

    const deps: GetProgressDeps = {
      listPublishedLessons: async () => published,
      listProgress: async () => rows,
      listExerciseScores: async () => [],
      getGamification: async () => gamification,
    };

    const result = await createGetProgressUseCase(deps)({ languageId: 'lang-1', userId: 'user-1' });
    expect(result.totalXp).toBe(25);
    expect(result.currentStreak).toBe(3);
    expect(result.longestStreak).toBe(5);
    expect(result.progressLevel).toBe(Level.Beginner);

    const beginner = result.byLevel.find((bucket) => bucket.level === Level.Beginner);
    expect(beginner).toEqual({
      level: Level.Beginner,
      category: null,
      completedCount: 1,
      totalCount: 2,
      percentage: 50,
    });

    const everyday = result.byCategory.find((bucket) => bucket.category === 'Everyday');
    expect(everyday?.completedCount).toBe(1);
    expect(everyday?.totalCount).toBe(2);
    expect(everyday?.percentage).toBe(50);

    expect(result.weakAreas).toEqual([
      {
        level: Level.Beginner,
        category: 'Everyday',
        averageScore: 40,
        sampleSize: 1,
      },
    ]);
  });

  it('omits buckets at or above the weak-area threshold', () => {
    const areas = identifyWeakAreas(
      [lesson('a', Level.Beginner, 'Everyday'), lesson('b', Level.Beginner, 'Work')],
      [
        { lessonId: 'a', completedAt: '2026-08-22T12:00:00.000Z', score: WEAK_AREA_SCORE_THRESHOLD },
        { lessonId: 'b', completedAt: '2026-08-22T12:00:00.000Z', score: 40 },
      ],
    );

    expect(areas).toEqual([
      { level: Level.Beginner, category: 'Work', averageScore: 40, sampleSize: 1 },
    ]);
    expect(roundToOneDecimal(100 / 3)).toBe(33.3);
  });

  it('uses exercise_results scores for weak areas when present', () => {
    const areas = identifyWeakAreas(
      [lesson('a', Level.Beginner, 'Everyday')],
      [{ lessonId: 'a', completedAt: '2026-08-22T12:00:00.000Z', score: 95 }],
      [
        { lessonId: 'a', score: 0 },
        { lessonId: 'a', score: 100 },
        { lessonId: 'a', score: 50 },
      ],
    );

    expect(areas).toEqual([
      { level: Level.Beginner, category: 'Everyday', averageScore: 50, sampleSize: 3 },
    ]);
  });
});
