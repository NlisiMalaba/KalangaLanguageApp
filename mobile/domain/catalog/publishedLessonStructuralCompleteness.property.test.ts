import * as fc from 'fast-check';

import { createGetLessonUseCase } from '@/domain/catalog/getLessonUseCase';
import { isPublishedLessonStructurallyComplete } from '@/domain/catalog/structuralCompleteness';
import type {
  AudioRef,
  LessonDetail,
  LessonExerciseDetail,
  LessonPhraseDetail,
  LessonVariationDetail,
} from '@/domain/catalog/types';
import { Level } from '@/domain/enums';

const languageId = '11111111-1111-7111-8111-111111111111';
const LEVELS = [Level.Beginner, Level.Intermediate, Level.Advanced] as const;

const textArb = fc
  .string({ minLength: 2, maxLength: 24 })
  .filter((value) => value.trim().length > 0);

const audioArb: fc.Arbitrary<AudioRef> = fc.record({
  id: fc.uuid(),
  cdnUrl: fc.constant('https://cdn.example/lesson.mp3'),
  fileFormat: fc.constant('Mp3'),
  speakerGender: fc.constant('Unspecified'),
  dialectLabel: fc.constant(null),
  durationMs: fc.integer({ min: 200, max: 8_000 }),
});

const variationArb: fc.Arbitrary<LessonVariationDetail> = fc.record({
  id: fc.uuid(),
  kalangaText: textArb,
  registerLabel: fc.constantFrom('formal', 'everyday'),
  audio: fc.array(audioArb, { minLength: 0, maxLength: 1 }),
});

const phraseArb: fc.Arbitrary<LessonPhraseDetail> = fc.record({
  id: fc.uuid(),
  kalangaText: textArb,
  englishTranslation: textArb,
  sortOrder: fc.nat({ max: 8 }),
  variations: fc.array(variationArb, { minLength: 0, maxLength: 2 }),
  audio: fc.array(audioArb, { minLength: 1, maxLength: 2 }),
});

const exerciseArb: fc.Arbitrary<LessonExerciseDetail> = fc.record({
  id: fc.uuid(),
  exerciseType: fc.constantFrom('Flashcard', 'MultipleChoice', 'SentenceBuilder', 'Listening'),
  promptData: textArb,
  correctAnswer: textArb,
  sortOrder: fc.nat({ max: 8 }),
});

const completeLessonArb: fc.Arbitrary<LessonDetail> = fc.record({
  id: fc.uuid(),
  languageId: fc.constant(languageId),
  title: textArb,
  level: fc.constantFrom(...LEVELS),
  category: fc.constantFrom('Everyday', 'Travel', 'Family', 'Work'),
  isScenario: fc.boolean(),
  scenarioContext: fc.option(textArb, { nil: null }),
  xpReward: fc.integer({ min: 5, max: 50 }),
  updatedAt: fc.constant('2026-08-23T12:00:00.000Z'),
  phrases: fc.array(phraseArb, { minLength: 1, maxLength: 3 }),
  exercises: fc.array(exerciseArb, { minLength: 1, maxLength: 3 }),
});

describe('published lesson structural completeness', () => {
  // Feature: kalanga-language-app, Property 7: Published Lesson Structural Completeness
  it('holds for locally stored published lessons and rejects incomplete graphs', async () => {
    await fc.assert(
      fc.asyncProperty(completeLessonArb, async (lesson) => {
        expect(isPublishedLessonStructurallyComplete(lesson)).toBe(true);
        expect(lesson.title.trim().length).toBeGreaterThan(0);
        expect(LEVELS).toContain(lesson.level);
        expect(lesson.category.trim().length).toBeGreaterThan(0);
        expect(lesson.phrases.length).toBeGreaterThan(0);
        expect(lesson.exercises.length).toBeGreaterThan(0);

        for (const phrase of lesson.phrases) {
          expect(phrase.kalangaText.trim().length).toBeGreaterThan(0);
          expect(phrase.englishTranslation.trim().length).toBeGreaterThan(0);
          expect(phrase.audio.length).toBeGreaterThan(0);
          for (const variation of phrase.variations) {
            expect(variation.registerLabel.trim().length).toBeGreaterThan(0);
          }
        }

        const getLessonApi = jest.fn();
        const getLesson = createGetLessonUseCase({
          loadLocalLesson: async () => lesson,
          catalogApi: { getLesson: getLessonApi },
          network: { isOnline: async () => false },
        });

        const loaded = await getLesson({ languageId, lessonId: lesson.id });
        expect(loaded).toEqual(lesson);
        expect(isPublishedLessonStructurallyComplete(loaded)).toBe(true);
        expect(getLessonApi).not.toHaveBeenCalled();

        expect(isPublishedLessonStructurallyComplete({ ...lesson, title: '   ' })).toBe(false);
        expect(isPublishedLessonStructurallyComplete({ ...lesson, category: '' })).toBe(false);
        expect(isPublishedLessonStructurallyComplete({ ...lesson, phrases: [] })).toBe(false);
        expect(isPublishedLessonStructurallyComplete({ ...lesson, exercises: [] })).toBe(false);
        expect(
          isPublishedLessonStructurallyComplete({
            ...lesson,
            phrases: lesson.phrases.map((phrase) => ({ ...phrase, audio: [] })),
          }),
        ).toBe(false);
        expect(
          isPublishedLessonStructurallyComplete({
            ...lesson,
            phrases: lesson.phrases.map((phrase) => ({
              ...phrase,
              kalangaText: '',
              englishTranslation: phrase.englishTranslation,
            })),
          }),
        ).toBe(false);

        const hasVariation = lesson.phrases.some((phrase) => phrase.variations.length > 0);
        if (hasVariation) {
          expect(
            isPublishedLessonStructurallyComplete({
              ...lesson,
              phrases: lesson.phrases.map((phrase) => ({
                ...phrase,
                variations: phrase.variations.map((variation) => ({ ...variation, registerLabel: '  ' })),
              })),
            }),
          ).toBe(false);
        }
      }),
      { numRuns: 100 },
    );
  });
});
