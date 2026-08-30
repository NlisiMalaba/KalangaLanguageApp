import { ExerciseType } from '@/domain/enums';
import { createExerciseEngine } from '@/domain/exercises/exerciseEngine';
import { ExerciseAttemptError, ExerciseStructureError } from '@/domain/exercises/errors';
import { gradeExercise } from '@/domain/exercises/gradeExercise';
import { isExerciseStructurallyValid, parseExercisePrompt } from '@/domain/exercises/parseExercisePrompt';
import type { ExerciseSource } from '@/domain/exercises/types';

function source(overrides: Partial<ExerciseSource> & Pick<ExerciseSource, 'exerciseType' | 'promptData'>): ExerciseSource {
  return {
    id: 'ex-1',
    languageId: 'lang-1',
    lessonId: 'lesson-1',
    correctAnswer: 'Hello',
    sortOrder: 0,
    ...overrides,
  };
}

const flashcard = source({
  exerciseType: ExerciseType.Flashcard,
  promptData: JSON.stringify({ phrase_id: 'phrase-1', kalanga_text: 'Mhoro' }),
  correctAnswer: 'Hello',
});

const multipleChoice = source({
  exerciseType: ExerciseType.MultipleChoice,
  promptData: JSON.stringify({
    phrase_id: 'phrase-1',
    prompt: 'Mhoro',
    options: ['Hello', 'Goodbye', 'Please', 'Thanks'],
    correct_index: 0,
  }),
  correctAnswer: 'Hello',
});

const listening = source({
  exerciseType: ExerciseType.Listening,
  promptData: JSON.stringify({
    phrase_id: 'phrase-1',
    recording_id: 'rec-1',
    options: ['Hello', 'Goodbye', 'Please', 'Thanks'],
    correct_index: 2,
  }),
  correctAnswer: 'Please',
});

const sentence = source({
  exerciseType: ExerciseType.SentenceBuilder,
  promptData: JSON.stringify({
    phrase_id: 'phrase-1',
    prompt: 'I want tomatoes',
    tokens: ['Ndinoda', 'tomato', 'zvino'],
    correct_order: [0, 1, 2],
  }),
  correctAnswer: 'Ndinoda tomato zvino',
});

describe('parseExercisePrompt', () => {
  it('accepts a flashcard with plain prompt text', () => {
    expect(isExerciseStructurallyValid(source({ exerciseType: ExerciseType.Flashcard, promptData: 'Mhoro' }))).toBe(
      true,
    );
  });

  it('rejects multiple choice without four options or a valid correct_index', () => {
    expect(
      isExerciseStructurallyValid(
        source({
          exerciseType: ExerciseType.MultipleChoice,
          promptData: JSON.stringify({ options: ['A', 'B'], correct_index: 0 }),
        }),
      ),
    ).toBe(false);
    expect(
      isExerciseStructurallyValid(
        source({
          exerciseType: ExerciseType.MultipleChoice,
          promptData: JSON.stringify({
            options: ['A', 'B', 'C', 'D'],
            correct_index: 4,
          }),
        }),
      ),
    ).toBe(false);
  });

  it('rejects sentence builders whose correct_order is not a permutation', () => {
    expect(
      isExerciseStructurallyValid(
        source({
          exerciseType: ExerciseType.SentenceBuilder,
          promptData: JSON.stringify({ tokens: ['a', 'b'], correct_order: [0, 0] }),
        }),
      ),
    ).toBe(false);
  });
});

describe('gradeExercise', () => {
  it('scores each exercise type', () => {
    expect(
      gradeExercise(parseExercisePrompt(flashcard)!, {
        kind: ExerciseType.Flashcard,
        translation: ' hello ',
      }).correct,
    ).toBe(true);
    expect(
      gradeExercise(parseExercisePrompt(multipleChoice)!, {
        kind: ExerciseType.MultipleChoice,
        selectedIndex: 1,
      }).correct,
    ).toBe(false);
    expect(
      gradeExercise(parseExercisePrompt(listening)!, { kind: ExerciseType.Listening, selectedIndex: 2 }).correct,
    ).toBe(true);
    expect(
      gradeExercise(parseExercisePrompt(sentence)!, { kind: ExerciseType.SentenceBuilder, order: [0, 2, 1] }).correct,
    ).toBe(false);
  });

  it('rejects an attempt that does not match the exercise type', () => {
    expect(() =>
      gradeExercise(parseExercisePrompt(flashcard)!, { kind: ExerciseType.MultipleChoice, selectedIndex: 0 }),
    ).toThrow(ExerciseAttemptError);
  });
});

describe('createExerciseEngine', () => {
  it('loads exercises from the local store only and flags a phrase for SRS when wrong', async () => {
    const flagPhraseForReview = jest.fn(async () => undefined);
    const listExercises = jest.fn(async () => [flashcard, multipleChoice]);
    const engine = createExerciseEngine({ listExercises, flagPhraseForReview });

    const loaded = await engine.loadLessonExercises('lang-1', 'lesson-1');
    expect(listExercises).toHaveBeenCalledWith('lang-1', 'lesson-1');
    expect(loaded).toHaveLength(2);
    expect(loaded[0]?.parsed.type).toBe(ExerciseType.Flashcard);
    expect(loaded[1]?.parsed.type).toBe(ExerciseType.MultipleChoice);

    const correct = await engine.submit({
      languageId: 'lang-1',
      userId: 'user-1',
      exercise: flashcard,
      attempt: { kind: ExerciseType.Flashcard, remembered: true },
    });
    expect(correct.correct).toBe(true);
    expect(flagPhraseForReview).not.toHaveBeenCalled();

    const wrong = await engine.submit({
      languageId: 'lang-1',
      userId: 'user-1',
      exercise: multipleChoice,
      attempt: { kind: ExerciseType.MultipleChoice, selectedIndex: 3 },
    });
    expect(wrong.correct).toBe(false);
    expect(wrong.revealedAnswer).toBe('Hello');
    expect(flagPhraseForReview).toHaveBeenCalledWith({
      languageId: 'lang-1',
      userId: 'user-1',
      phraseId: 'phrase-1',
    });
  });

  it('rejects structurally invalid exercises', async () => {
    const engine = createExerciseEngine({
      listExercises: async () => [],
      flagPhraseForReview: async () => undefined,
    });

    await expect(
      engine.submit({
        languageId: 'lang-1',
        userId: 'user-1',
        exercise: source({
          exerciseType: ExerciseType.MultipleChoice,
          promptData: 'not-json',
        }),
        attempt: { kind: ExerciseType.MultipleChoice, selectedIndex: 0 },
      }),
    ).rejects.toBeInstanceOf(ExerciseStructureError);
  });
});
