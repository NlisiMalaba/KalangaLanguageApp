import { WEAK_AREA_SCORE_THRESHOLD } from '@/constants/progress';
import { Level } from '@/domain/enums';
import type {
  LearnerProgressSnapshot,
  ProgressBucket,
  ExerciseScoreSample,
  ProgressCompletion,
  ProgressLessonSummary,
  WeakArea,
} from '@/domain/progress/types';

const LEVEL_ORDER: Record<Level, number> = {
  [Level.Beginner]: 0,
  [Level.Intermediate]: 1,
  [Level.Advanced]: 2,
};

export function roundToOneDecimal(value: number): number {
  return Math.round(value * 10 + Number.EPSILON) / 10;
}

function completionPercentage(completed: number, total: number): number {
  if (total === 0) {
    return 0;
  }

  return roundToOneDecimal((100 * completed) / total);
}

function completedByLesson(rows: readonly ProgressCompletion[]): Map<string, ProgressCompletion> {
  const map = new Map<string, ProgressCompletion>();
  for (const row of rows) {
    if (row.completedAt != null) {
      map.set(row.lessonId, row);
    }
  }
  return map;
}

function toBucket(
  level: Level | null,
  category: string | null,
  lessons: readonly ProgressLessonSummary[],
  completed: ReadonlyMap<string, ProgressCompletion>,
): ProgressBucket {
  const totalCount = lessons.length;
  const completedCount = lessons.filter((lesson) => completed.has(lesson.id)).length;
  return {
    level,
    category,
    completedCount,
    totalCount,
    percentage: completionPercentage(completedCount, totalCount),
  };
}

export function bucketsByLevel(
  published: readonly ProgressLessonSummary[],
  completions: readonly ProgressCompletion[],
): ProgressBucket[] {
  const completed = completedByLesson(completions);
  const groups = new Map<Level, ProgressLessonSummary[]>();
  for (const lesson of published) {
    const group = groups.get(lesson.level) ?? [];
    group.push(lesson);
    groups.set(lesson.level, group);
  }

  return [...groups.entries()]
    .sort(([left], [right]) => LEVEL_ORDER[left] - LEVEL_ORDER[right])
    .map(([level, lessons]) => toBucket(level, null, lessons, completed));
}

export function bucketsByCategory(
  published: readonly ProgressLessonSummary[],
  completions: readonly ProgressCompletion[],
): ProgressBucket[] {
  const completed = completedByLesson(completions);
  const groups = new Map<string, ProgressLessonSummary[]>();
  for (const lesson of published) {
    const group = groups.get(lesson.category) ?? [];
    group.push(lesson);
    groups.set(lesson.category, group);
  }

  return [...groups.entries()]
    .sort(([left], [right]) => left.localeCompare(right))
    .map(([category, lessons]) => toBucket(null, category, lessons, completed));
}

export function identifyWeakAreas(
  published: readonly ProgressLessonSummary[],
  completions: readonly ProgressCompletion[],
  exerciseScores: readonly ExerciseScoreSample[] = [],
): WeakArea[] {
  const completed = completedByLesson(completions);
  const groups = new Map<string, ProgressLessonSummary[]>();
  for (const lesson of published) {
    const key = `${lesson.level}\0${lesson.category}`;
    const group = groups.get(key) ?? [];
    group.push(lesson);
    groups.set(key, group);
  }

  const areas: WeakArea[] = [];
  for (const lessons of groups.values()) {
    const lessonIds = new Set(lessons.map((item) => item.id));
    const fromExercises = exerciseScores
      .filter((sample) => lessonIds.has(sample.lessonId))
      .map((sample) => sample.score);
    const fromCompletions = lessons
      .map((lesson) => completed.get(lesson.id))
      .filter((row): row is ProgressCompletion => row != null && row.score != null)
      .map((row) => row.score as number);
    const scores = fromExercises.length > 0 ? fromExercises : fromCompletions;

    if (scores.length === 0) {
      continue;
    }

    const averageScore = roundToOneDecimal(scores.reduce((sum, score) => sum + score, 0) / scores.length);
    if (averageScore >= WEAK_AREA_SCORE_THRESHOLD) {
      continue;
    }

    areas.push({
      level: lessons[0].level,
      category: lessons[0].category,
      averageScore,
      sampleSize: scores.length,
    });
  }

  return areas.sort((left, right) => {
    if (left.averageScore !== right.averageScore) {
      return left.averageScore - right.averageScore;
    }
    if (left.level !== right.level) {
      return LEVEL_ORDER[left.level] - LEVEL_ORDER[right.level];
    }
    return left.category.localeCompare(right.category);
  });
}

export function emptyGamificationSnapshot(progressLevel: Level = Level.Beginner): LearnerProgressSnapshot {
  return {
    totalXp: 0,
    currentStreak: 0,
    longestStreak: 0,
    progressLevel,
    lastActivityDate: null,
  };
}
