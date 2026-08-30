import {
  DEFAULT_SRS_INTERVAL_DAYS,
  MAX_EASE_FACTOR,
  MIN_EASE_FACTOR,
  SRS_CORRECT_QUALITY,
  SRS_INCORRECT_QUALITY,
  SRS_PASSING_QUALITY,
  SRS_SECOND_INTERVAL_DAYS,
} from '@/constants/srs';
import type { SpacedRepetitionRecord } from '@/domain/entities';
import { addCalendarDays, utcCalendarDate } from '@/domain/srs/calendarDate';

function roundAwayFromZero(value: number, decimals = 0): number {
  const factor = 10 ** decimals;
  return (Math.sign(value) * Math.round(Math.abs(value) * factor)) / factor;
}

function nextInterval(intervalDays: number, easeFactor: number): number {
  const next = roundAwayFromZero(intervalDays * easeFactor);
  return Math.max(intervalDays + 1, next);
}

function adjustEase(easeFactor: number, quality: number): number {
  const delta = 5 - quality;
  const adjusted = easeFactor + (0.1 - delta * (0.08 + delta * 0.02));
  const rounded = roundAwayFromZero(adjusted, 2);
  if (rounded < MIN_EASE_FACTOR) {
    return MIN_EASE_FACTOR;
  }

  if (rounded > MAX_EASE_FACTOR) {
    return MAX_EASE_FACTOR;
  }

  return rounded;
}

export function recordAnswer(
  record: SpacedRepetitionRecord,
  isCorrect: boolean,
  utcNow: Date,
): SpacedRepetitionRecord {
  const quality = isCorrect ? SRS_CORRECT_QUALITY : SRS_INCORRECT_QUALITY;
  const ease = adjustEase(record.easeFactor, quality);
  let interval = record.intervalDays;
  let repetitions = record.repetitions;

  if (quality < SRS_PASSING_QUALITY) {
    repetitions = 0;
    interval = DEFAULT_SRS_INTERVAL_DAYS;
  } else if (repetitions === 0) {
    interval = DEFAULT_SRS_INTERVAL_DAYS;
    repetitions = 1;
  } else if (repetitions === 1) {
    interval = SRS_SECOND_INTERVAL_DAYS;
    repetitions = 2;
  } else {
    interval = nextInterval(interval, ease);
    repetitions += 1;
  }

  const today = utcCalendarDate(utcNow);
  const instant = utcNow.toISOString();
  return {
    ...record,
    easeFactor: ease,
    intervalDays: interval,
    repetitions,
    nextReviewAt: addCalendarDays(today, interval),
    lastReviewedAt: instant,
    updatedAt: instant,
  };
}
