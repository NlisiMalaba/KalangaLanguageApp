import { DEFAULT_LESSON_XP_REWARD } from '@/constants/contributor';
import { MULTIPLE_CHOICE_OPTION_COUNT, MIN_SENTENCE_TOKENS } from '@/constants/exercises';
import { ContributorValidationError } from '@/domain/contributor/errors';
import { encodeExercisePrompt } from '@/domain/contributor/encodeExercisePrompt';
import type { DraftExercise, DraftPhrase, LessonDraft } from '@/domain/contributor/types';
import { ExerciseType, Level } from '@/domain/enums';
import { isExerciseStructurallyValid } from '@/domain/exercises/parseExercisePrompt';

export function assertLessonDraftValid(draft: LessonDraft): void {
  if (draft.title.trim().length === 0) {
    throw new ContributorValidationError('Title is required.');
  }

  if (draft.category.trim().length === 0) {
    throw new ContributorValidationError('Category is required.');
  }

  if (draft.xpReward < 1) {
    throw new ContributorValidationError('XP reward must be at least 1.');
  }

  if (draft.isScenario && draft.scenarioContext.trim().length === 0) {
    throw new ContributorValidationError('Scenario lessons need a situation description.');
  }

  for (const phrase of draft.phrases) {
    if (phrase.kalangaText.trim().length === 0 || phrase.englishTranslation.trim().length === 0) {
      throw new ContributorValidationError('Each phrase needs Kalanga text and an English translation.');
    }

    for (const variation of phrase.variations) {
      if (variation.kalangaText.trim().length === 0 || variation.registerLabel.trim().length === 0) {
        throw new ContributorValidationError('Each variation needs Kalanga text and a register label.');
      }
    }
  }

  for (const exercise of draft.exercises) {
    if (
      (exercise.exerciseType === ExerciseType.MultipleChoice || exercise.exerciseType === ExerciseType.Listening) &&
      exercise.options.length !== MULTIPLE_CHOICE_OPTION_COUNT
    ) {
      throw new ContributorValidationError(`Multiple choice needs ${MULTIPLE_CHOICE_OPTION_COUNT} options.`);
    }

    if (exercise.exerciseType === ExerciseType.SentenceBuilder) {
      const tokens = exercise.tokens.split(/\s+/).filter((token) => token.length > 0);
      if (tokens.length < MIN_SENTENCE_TOKENS) {
        throw new ContributorValidationError(`Sentence builder needs at least ${MIN_SENTENCE_TOKENS} tokens.`);
      }
    }

    const encoded = encodeExercisePrompt(exercise);
    if (
      !isExerciseStructurallyValid({
        id: exercise.clientKey,
        languageId: 'lang',
        lessonId: 'lesson',
        exerciseType: exercise.exerciseType,
        promptData: encoded.promptData,
        correctAnswer: encoded.correctAnswer,
        sortOrder: exercise.sortOrder,
      })
    ) {
      throw new ContributorValidationError('One or more exercises are missing required fields.');
    }
  }
}

export function emptyPhrase(): DraftPhrase {
  return {
    clientKey: newDraftKey(),
    id: null,
    kalangaText: '',
    englishTranslation: '',
    sortOrder: 0,
    variations: [],
    audio: [],
  };
}

export function emptyExercise(exerciseType: ExerciseType = ExerciseType.Flashcard): DraftExercise {
  return {
    clientKey: newDraftKey(),
    id: null,
    exerciseType,
    prompt: '',
    correctAnswer: '',
    options: ['', '', '', ''],
    correctIndex: 0,
    tokens: '',
    recordingId: null,
    phraseId: null,
    sortOrder: 0,
  };
}

function newDraftKey(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `draft-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function emptyLessonDraft(): LessonDraft {
  return {
    id: null,
    title: '',
    level: Level.Beginner,
    category: 'Everyday',
    isScenario: false,
    scenarioContext: '',
    xpReward: DEFAULT_LESSON_XP_REWARD,
    status: null,
    phrases: [],
    exercises: [],
  };
}
