import { MULTIPLE_CHOICE_OPTION_COUNT, MIN_SENTENCE_TOKENS } from '@/constants/exercises';
import { ExerciseType } from '@/domain/enums';
import type { EntityId } from '@/domain/entities';

export type ExerciseSource = {
  id: EntityId;
  languageId: EntityId;
  lessonId: EntityId;
  exerciseType: string;
  promptData: string;
  correctAnswer: string;
  sortOrder: number;
};

export type FlashcardPrompt = {
  type: typeof ExerciseType.Flashcard;
  phraseId: EntityId | null;
  kalangaText: string;
  correctAnswer: string;
};

export type ChoicePrompt = {
  type: typeof ExerciseType.MultipleChoice | typeof ExerciseType.Listening;
  phraseId: EntityId | null;
  prompt: string;
  options: readonly string[];
  correctIndex: number;
  recordingId: EntityId | null;
  correctAnswer: string;
};

export type SentenceBuilderPrompt = {
  type: typeof ExerciseType.SentenceBuilder;
  phraseId: EntityId | null;
  prompt: string;
  tokens: readonly string[];
  correctOrder: readonly number[];
  correctAnswer: string;
};

export type ParsedExercise = FlashcardPrompt | ChoicePrompt | SentenceBuilderPrompt;

export type ExerciseAttempt =
  | { kind: typeof ExerciseType.Flashcard; translation?: string; remembered?: boolean }
  | { kind: typeof ExerciseType.MultipleChoice; selectedIndex: number }
  | { kind: typeof ExerciseType.Listening; selectedIndex: number }
  | { kind: typeof ExerciseType.SentenceBuilder; order: readonly number[] };

export type GradeResult = {
  correct: boolean;
  revealedAnswer: string;
  phraseId: EntityId | null;
};

export type FlagPhraseForReview = (input: {
  languageId: EntityId;
  userId: EntityId;
  phraseId: EntityId;
}) => Promise<void>;

export type RecordExerciseResult = (input: {
  languageId: EntityId;
  userId: EntityId;
  exerciseId: EntityId;
  lessonId: EntityId;
  isCorrect: boolean;
  score: number;
  answeredAt: string;
}) => Promise<void>;

export type ListLessonExercises = (
  languageId: EntityId,
  lessonId: EntityId,
) => Promise<ExerciseSource[]>;

export type ExerciseEngineDeps = {
  listExercises: ListLessonExercises;
  flagPhraseForReview: FlagPhraseForReview;
  recordExerciseResult?: RecordExerciseResult;
};

export function isChoiceType(
  type: string,
): type is typeof ExerciseType.MultipleChoice | typeof ExerciseType.Listening {
  return type === ExerciseType.MultipleChoice || type === ExerciseType.Listening;
}

export function hasRequiredChoiceCount(options: readonly string[]): boolean {
  return options.length === MULTIPLE_CHOICE_OPTION_COUNT;
}

export function hasRequiredSentenceLength(tokens: readonly string[]): boolean {
  return tokens.length >= MIN_SENTENCE_TOKENS;
}
