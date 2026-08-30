import * as fc from 'fast-check';

import { MIN_SENTENCE_TOKENS, MULTIPLE_CHOICE_OPTION_COUNT } from '@/constants/exercises';
import { ExerciseType } from '@/domain/enums';
import { isExerciseStructurallyValid, parseExercisePrompt } from '@/domain/exercises/parseExercisePrompt';
import type { ChoicePrompt, ExerciseSource, SentenceBuilderPrompt } from '@/domain/exercises/types';

const textArb = fc
  .string({ minLength: 1, maxLength: 24 })
  .filter((value) => value.trim().length > 0);

const uniqueTokensArb = (minLength: number, maxLength: number) =>
  fc.uniqueArray(textArb, {
    minLength,
    maxLength,
    selector: (value) => value.trim(),
  });

function baseExercise(exerciseType: string, promptData: string, correctAnswer: string): ExerciseSource {
  return {
    id: '11111111-1111-7111-8111-111111111111',
    languageId: '11111111-1111-7111-8111-111111111112',
    lessonId: '11111111-1111-7111-8111-111111111113',
    exerciseType,
    promptData,
    correctAnswer,
    sortOrder: 0,
  };
}

const flashcardArb: fc.Arbitrary<ExerciseSource> = fc
  .tuple(textArb, textArb, fc.option(fc.uuid(), { nil: null }))
  .map(([kalanga, answer, phraseId]) =>
    baseExercise(
      ExerciseType.Flashcard,
      JSON.stringify({ phrase_id: phraseId, kalanga_text: kalanga }),
      answer,
    ),
  );

const choiceArb = (type: typeof ExerciseType.MultipleChoice | typeof ExerciseType.Listening) =>
  fc
    .tuple(
      uniqueTokensArb(MULTIPLE_CHOICE_OPTION_COUNT, MULTIPLE_CHOICE_OPTION_COUNT),
      fc.integer({ min: 0, max: MULTIPLE_CHOICE_OPTION_COUNT - 1 }),
      textArb,
    )
    .map(([options, correctIndex, prompt]) =>
      baseExercise(
        type,
        JSON.stringify({ phrase_id: 'phrase-1', prompt, options, correct_index: correctIndex }),
        options[correctIndex] ?? options[0] ?? 'answer',
      ),
    );

const sentenceArb: fc.Arbitrary<ExerciseSource> = uniqueTokensArb(MIN_SENTENCE_TOKENS, 6).chain((tokens) => {
  const indices = tokens.map((_, index) => index);
  return fc.shuffledSubarray(indices, { minLength: tokens.length, maxLength: tokens.length }).map((correctOrder) =>
    baseExercise(
      ExerciseType.SentenceBuilder,
      JSON.stringify({ phrase_id: 'phrase-1', prompt: 'Build', tokens, correct_order: correctOrder }),
      correctOrder.map((index) => tokens[index]).join(' '),
    ),
  );
});

const validArb = fc.oneof(
  flashcardArb,
  choiceArb(ExerciseType.MultipleChoice),
  choiceArb(ExerciseType.Listening),
  sentenceArb,
);

describe('exercise structural validity per type', () => {
  // Feature: kalanga-language-app, Property 13: Exercise Structural Validity Per Type
  it('accepts well-formed exercises of each type and rejects type-specific structural gaps', () => {
    fc.assert(
      fc.property(validArb, (exercise) => {
        expect(isExerciseStructurallyValid(exercise)).toBe(true);
        const parsed = parseExercisePrompt(exercise);
        expect(parsed).not.toBeNull();
        if (!parsed) {
          return;
        }

        expect(parsed.type).toBe(exercise.exerciseType);

        if (parsed.type === ExerciseType.Flashcard) {
          expect(parsed.kalangaText.trim().length).toBeGreaterThan(0);
          expect(parsed.correctAnswer.trim().length).toBeGreaterThan(0);
          expect(
            isExerciseStructurallyValid({ ...exercise, correctAnswer: '   ' }),
          ).toBe(false);
        }

        if (parsed.type === ExerciseType.MultipleChoice || parsed.type === ExerciseType.Listening) {
          const choice = parsed as ChoicePrompt;
          expect(choice.options).toHaveLength(MULTIPLE_CHOICE_OPTION_COUNT);
          expect(choice.correctIndex).toBeGreaterThanOrEqual(0);
          expect(choice.correctIndex).toBeLessThan(MULTIPLE_CHOICE_OPTION_COUNT);

          const body = JSON.parse(exercise.promptData) as { options: string[]; correct_index: number };
          expect(
            isExerciseStructurallyValid({
              ...exercise,
              promptData: JSON.stringify({ ...body, options: body.options.slice(0, 3) }),
            }),
          ).toBe(false);
          expect(
            isExerciseStructurallyValid({
              ...exercise,
              promptData: JSON.stringify({ ...body, correct_index: MULTIPLE_CHOICE_OPTION_COUNT }),
            }),
          ).toBe(false);
          expect(
            isExerciseStructurallyValid({
              ...exercise,
              promptData: JSON.stringify({ ...body, options: ['', ...body.options.slice(1)] }),
            }),
          ).toBe(false);
        }

        if (parsed.type === ExerciseType.SentenceBuilder) {
          const sentence = parsed as SentenceBuilderPrompt;
          expect(sentence.tokens.length).toBeGreaterThanOrEqual(MIN_SENTENCE_TOKENS);
          expect(sentence.correctOrder).toHaveLength(sentence.tokens.length);
          expect(new Set(sentence.correctOrder).size).toBe(sentence.tokens.length);

          const body = JSON.parse(exercise.promptData) as { tokens: string[]; correct_order: number[] };
          expect(
            isExerciseStructurallyValid({
              ...exercise,
              promptData: JSON.stringify({ tokens: [body.tokens[0]], correct_order: [0] }),
            }),
          ).toBe(false);
          expect(
            isExerciseStructurallyValid({
              ...exercise,
              promptData: JSON.stringify({
                tokens: body.tokens,
                correct_order: body.correct_order.map(() => 0),
              }),
            }),
          ).toBe(false);
        }

        expect(isExerciseStructurallyValid({ ...exercise, exerciseType: 'Unknown' })).toBe(false);
      }),
      { numRuns: 100 },
    );
  });
});
