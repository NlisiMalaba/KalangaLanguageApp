import { MULTIPLE_CHOICE_OPTION_COUNT } from '@/constants/exercises';
import { ExerciseType } from '@/domain/enums';
import type { DraftExercise } from '@/domain/contributor/types';

export function encodeExercisePrompt(exercise: DraftExercise): { promptData: string; correctAnswer: string } {
  if (exercise.exerciseType === ExerciseType.Flashcard) {
    const kalangaText = exercise.prompt.trim();
    const correctAnswer = exercise.correctAnswer.trim();
    return {
      promptData: JSON.stringify({
        phrase_id: exercise.phraseId,
        kalanga_text: kalangaText,
        prompt: kalangaText,
      }),
      correctAnswer,
    };
  }

  if (
    exercise.exerciseType === ExerciseType.MultipleChoice ||
    exercise.exerciseType === ExerciseType.Listening
  ) {
    const options = exercise.options.map((option) => option.trim());
    const correctIndex = Math.min(MULTIPLE_CHOICE_OPTION_COUNT - 1, Math.max(0, exercise.correctIndex));
    return {
      promptData: JSON.stringify({
        phrase_id: exercise.phraseId,
        prompt: exercise.prompt.trim(),
        options,
        correct_index: correctIndex,
        recording_id: exercise.recordingId,
      }),
      correctAnswer: options[correctIndex] ?? exercise.correctAnswer.trim(),
    };
  }

  const tokens = exercise.tokens
    .split(/\s+/)
    .map((token) => token.trim())
    .filter((token) => token.length > 0);
  const correctOrder = tokens.map((_, index) => index);
  return {
    promptData: JSON.stringify({
      phrase_id: exercise.phraseId,
      prompt: exercise.prompt.trim(),
      tokens,
      correct_order: correctOrder,
    }),
    correctAnswer: tokens.join(' '),
  };
}
