import { CatalogApiError } from '@/domain/catalog/errors';
import { ExerciseType, Level } from '@/domain/enums';
import { mapBrowseLessonCatalogResponse } from '@/lib/catalog/httpCatalogApi';
import { mapGetLessonResponse } from '@/lib/catalog/mapGetLessonResponse';

describe('mapBrowseLessonCatalogResponse', () => {
  it('unwraps nested value-object ids and numeric levels', () => {
    const items = mapBrowseLessonCatalogResponse({
      lessons: [
        {
          lessonId: { value: 'lesson-1' },
          languageId: { value: 'lang-1' },
          title: 'Greetings',
          level: 0,
          category: 'Everyday',
          isScenario: false,
          scenarioContext: null,
          xpReward: 10,
          updatedAt: '2026-08-23T12:00:00+00:00',
        },
      ],
    });

    expect(items[0]).toMatchObject({
      id: 'lesson-1',
      languageId: 'lang-1',
      level: Level.Beginner,
      category: 'Everyday',
    });
  });

  it('rejects a payload without lessons', () => {
    expect(() => mapBrowseLessonCatalogResponse({})).toThrow(CatalogApiError);
  });
});

describe('mapGetLessonResponse', () => {
  it('maps nested lesson, phrase, variation, and exercise payloads', () => {
    const detail = mapGetLessonResponse({
      lesson: {
        lessonId: { value: 'lesson-1' },
        languageId: { value: 'lang-1' },
        title: 'Greetings',
        level: 0,
        category: 'Everyday',
        isScenario: true,
        scenarioContext: 'A greeting.',
        xpReward: 10,
        updatedAt: '2026-08-23T12:00:00+00:00',
        phrases: [
          {
            phraseId: { value: 'phrase-1' },
            kalangaText: 'Mhoro',
            englishTranslation: 'Hello',
            sortOrder: 0,
            variations: [
              {
                variationId: { value: 'var-1' },
                kalangaText: 'Mhoro',
                registerLabel: 'Casual',
                audio: [],
              },
            ],
            audio: [],
          },
        ],
        exercises: [
          {
            exerciseId: { value: 'ex-1' },
            exerciseType: 0,
            promptData: 'Hello',
            correctAnswer: 'Mhoro',
            sortOrder: 0,
          },
        ],
      },
    });

    expect(detail.title).toBe('Greetings');
    expect(detail.phrases[0]?.variations[0]?.registerLabel).toBe('Casual');
    expect(detail.exercises[0]?.exerciseType).toBe(ExerciseType.Flashcard);
  });
});
