import { ExerciseType } from '@/domain/enums';
import type { EntityId, Phrase, SpacedRepetitionRecord } from '@/domain/entities';
import type { ExerciseSource } from '@/domain/exercises/types';
import { prepareExercises, type PreparedExercise } from '@/domain/exercises/prepareExercises';

export type ReviewPrompt = {
  phrase: Phrase;
  kalangaText: string;
  card: SpacedRepetitionRecord;
};

export function flashcardFromReviewPrompt(prompt: ReviewPrompt, sortOrder: number): ExerciseSource {
  return {
    id: prompt.card.id,
    languageId: prompt.phrase.languageId,
    lessonId: prompt.phrase.lessonId,
    exerciseType: ExerciseType.Flashcard,
    promptData: JSON.stringify({
      phrase_id: prompt.phrase.id,
      kalanga_text: prompt.kalangaText,
    }),
    correctAnswer: prompt.phrase.englishTranslation,
    sortOrder,
  };
}

export function prepareReviewExercises(prompts: readonly ReviewPrompt[]): PreparedExercise[] {
  return prepareExercises(prompts.map((prompt, index) => flashcardFromReviewPrompt(prompt, index)));
}

export type LoadReviewPrompts = (input: {
  languageId: EntityId;
  userId: EntityId;
  date: string;
}) => Promise<ReviewPrompt[]>;
