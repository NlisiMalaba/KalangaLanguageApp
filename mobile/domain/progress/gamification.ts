import { ADVANCED_XP_THRESHOLD, INTERMEDIATE_XP_THRESHOLD } from '@/constants/progress';
import type { CalendarDate, EntityId, LearnerGamification } from '@/domain/entities';
import { Level } from '@/domain/enums';
import { addCalendarDays } from '@/domain/srs/calendarDate';
import { newGamificationId } from '@/domain/progress/ids';

export function createLearnerGamification(input: {
  languageId: EntityId;
  userId: EntityId;
  now: Date;
}): LearnerGamification {
  return {
    id: newGamificationId(),
    languageId: input.languageId,
    userId: input.userId,
    totalXp: 0,
    currentStreak: 0,
    longestStreak: 0,
    lastActivityDate: null,
    progressLevel: Level.Beginner,
    updatedAt: input.now.toISOString(),
  };
}

export function awardXp(state: LearnerGamification, xp: number, now: Date): LearnerGamification {
  if (xp <= 0) {
    return state;
  }

  return {
    ...state,
    totalXp: state.totalXp + xp,
    updatedAt: now.toISOString(),
  };
}

export function recordActivity(
  state: LearnerGamification,
  activityDate: CalendarDate,
  now: Date,
): LearnerGamification {
  if (state.lastActivityDate === activityDate) {
    return { ...state, updatedAt: now.toISOString() };
  }

  const currentStreak =
    state.lastActivityDate != null && addCalendarDays(state.lastActivityDate, 1) === activityDate
      ? state.currentStreak + 1
      : 1;

  return {
    ...state,
    currentStreak,
    longestStreak: Math.max(state.longestStreak, currentStreak),
    lastActivityDate: activityDate,
    updatedAt: now.toISOString(),
  };
}

export function advanceLevelIfThresholdCrossed(
  state: LearnerGamification,
  now: Date,
): LearnerGamification {
  const next =
    state.totalXp >= ADVANCED_XP_THRESHOLD
      ? Level.Advanced
      : state.totalXp >= INTERMEDIATE_XP_THRESHOLD
        ? Level.Intermediate
        : Level.Beginner;

  if (next === state.progressLevel) {
    return state;
  }

  return { ...state, progressLevel: next, updatedAt: now.toISOString() };
}
