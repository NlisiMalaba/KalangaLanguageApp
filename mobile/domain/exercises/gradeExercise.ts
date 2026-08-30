import { ExerciseType } from '@/domain/enums';
import { ExerciseAttemptError } from '@/domain/exercises/errors';
import type { ExerciseAttempt, GradeResult, ParsedExercise } from '@/domain/exercises/types';

function normalize(value: string): string {
  return value.trim().replace(/\s+/g, ' ').toLowerCase();
}

function sameOrder(left: readonly number[], right: readonly number[]): boolean {
  if (left.length !== right.length) {
    return false;
  }

  return left.every((value, index) => value === right[index]);
}

export function gradeExercise(exercise: ParsedExercise, attempt: ExerciseAttempt): GradeResult {
  if (attempt.kind !== exercise.type) {
    throw new ExerciseAttemptError();
  }

  const phraseId = exercise.phraseId;
  const revealedAnswer = exercise.correctAnswer;

  if (exercise.type === ExerciseType.Flashcard && attempt.kind === ExerciseType.Flashcard) {
    if (typeof attempt.remembered === 'boolean') {
      return { correct: attempt.remembered, revealedAnswer, phraseId };
    }

    const translation = attempt.translation ?? '';
    return { correct: normalize(translation) === normalize(exercise.correctAnswer), revealedAnswer, phraseId };
  }

  if (
    (exercise.type === ExerciseType.MultipleChoice || exercise.type === ExerciseType.Listening) &&
    (attempt.kind === ExerciseType.MultipleChoice || attempt.kind === ExerciseType.Listening)
  ) {
    return { correct: attempt.selectedIndex === exercise.correctIndex, revealedAnswer, phraseId };
  }

  if (exercise.type === ExerciseType.SentenceBuilder && attempt.kind === ExerciseType.SentenceBuilder) {
    return { correct: sameOrder(attempt.order, exercise.correctOrder), revealedAnswer, phraseId };
  }

  throw new ExerciseAttemptError();
}
