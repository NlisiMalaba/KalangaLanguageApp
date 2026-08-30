import { MULTIPLE_CHOICE_OPTION_COUNT } from '@/constants/exercises';
import { ExerciseType } from '@/domain/enums';
import type { EntityId } from '@/domain/entities';
import {
  hasRequiredChoiceCount,
  hasRequiredSentenceLength,
  isChoiceType,
  type ExerciseSource,
  type ParsedExercise,
} from '@/domain/exercises/types';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function readString(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : null;
}

function readId(value: unknown): EntityId | null {
  return readString(value);
}

function readIndex(value: unknown): number | null {
  if (typeof value !== 'number' || !Number.isInteger(value)) {
    return null;
  }

  return value;
}

function readStringArray(value: unknown): string[] | null {
  if (!Array.isArray(value) || value.some((item) => typeof item !== 'string')) {
    return null;
  }

  return value.map((item) => item.trim());
}

function readIndexArray(value: unknown): number[] | null {
  if (!Array.isArray(value) || value.some((item) => typeof item !== 'number' || !Number.isInteger(item))) {
    return null;
  }

  return value;
}

function isPermutation(order: readonly number[], length: number): boolean {
  if (order.length !== length) {
    return false;
  }

  const seen = new Set<number>();
  for (const index of order) {
    if (index < 0 || index >= length || seen.has(index)) {
      return false;
    }

    seen.add(index);
  }

  return seen.size === length;
}

function parseJsonObject(raw: string): Record<string, unknown> | null {
  try {
    const parsed: unknown = JSON.parse(raw);
    return isRecord(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

/**
 * Property 13: each exercise type has the fields needed to score it offline
 * (phrase prompt, four options + correct_index, or tokens + correct_order).
 */
export function parseExercisePrompt(exercise: ExerciseSource): ParsedExercise | null {
  const type = exercise.exerciseType;
  const json = parseJsonObject(exercise.promptData);

  if (type === ExerciseType.Flashcard) {
    const kalangaText = json
      ? readString(json.kalanga_text) ?? readString(json.prompt)
      : readString(exercise.promptData);
    const correctAnswer = readString(exercise.correctAnswer);
    if (!kalangaText || !correctAnswer) {
      return null;
    }

    return {
      type,
      phraseId: json ? readId(json.phrase_id) : null,
      kalangaText,
      correctAnswer,
    };
  }

  if (!json) {
    return null;
  }

  const phraseId = readId(json.phrase_id);
  const correctAnswer = readString(exercise.correctAnswer) ?? '';

  if (isChoiceType(type)) {
    const options = readStringArray(json.options);
    const correctIndex = readIndex(json.correct_index);
    const prompt = readString(json.prompt) ?? readString(json.kalanga_text) ?? '';
    if (
      !options ||
      !hasRequiredChoiceCount(options) ||
      options.some((option) => option.length === 0) ||
      correctIndex === null ||
      correctIndex < 0 ||
      correctIndex >= MULTIPLE_CHOICE_OPTION_COUNT
    ) {
      return null;
    }

    return {
      type,
      phraseId,
      prompt,
      options,
      correctIndex,
      recordingId: readId(json.recording_id),
      correctAnswer: correctAnswer || options[correctIndex] || '',
    };
  }

  if (type === ExerciseType.SentenceBuilder) {
    const tokens = readStringArray(json.tokens);
    const correctOrder = readIndexArray(json.correct_order);
    if (
      !tokens ||
      !correctOrder ||
      !hasRequiredSentenceLength(tokens) ||
      tokens.some((token) => token.length === 0) ||
      !isPermutation(correctOrder, tokens.length)
    ) {
      return null;
    }

    const assembled = correctOrder.map((index) => tokens[index]).join(' ');
    return {
      type,
      phraseId,
      prompt: readString(json.prompt) ?? '',
      tokens,
      correctOrder,
      correctAnswer: correctAnswer || assembled,
    };
  }

  return null;
}

export function isExerciseStructurallyValid(exercise: ExerciseSource): boolean {
  return parseExercisePrompt(exercise) !== null;
}
