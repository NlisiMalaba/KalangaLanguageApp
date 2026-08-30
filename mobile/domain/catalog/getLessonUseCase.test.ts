import { createGetLessonUseCase } from '@/domain/catalog/getLessonUseCase';
import { LessonNotFoundError } from '@/domain/catalog/errors';
import type { GetLessonDeps } from '@/domain/catalog/ports';
import type { LessonDetail } from '@/domain/catalog/types';
import { Level } from '@/domain/enums';

const languageId = '11111111-1111-7111-8111-111111111111';

function detail(overrides: Partial<LessonDetail> = {}): LessonDetail {
  return {
    id: 'lesson-1',
    languageId,
    title: 'Greetings',
    level: Level.Beginner,
    category: 'Everyday',
    isScenario: false,
    scenarioContext: null,
    xpReward: 10,
    updatedAt: '2026-08-23T12:00:00.000Z',
    phrases: [
      {
        id: 'phrase-1',
        kalangaText: 'Mhoro',
        englishTranslation: 'Hello',
        sortOrder: 0,
        variations: [],
        audio: [
          {
            id: 'audio-1',
            cdnUrl: 'https://cdn.example/audio-1.mp3',
            fileFormat: 'Mp3',
            speakerGender: 'Unspecified',
            dialectLabel: null,
            durationMs: 800,
          },
        ],
      },
    ],
    exercises: [
      {
        id: 'exercise-1',
        exerciseType: 'Flashcard',
        promptData: 'Say hello',
        correctAnswer: 'Mhoro',
        sortOrder: 0,
      },
    ],
    ...overrides,
  };
}

function createDeps(overrides: Partial<GetLessonDeps> = {}): GetLessonDeps {
  return {
    loadLocalLesson: async () => null,
    catalogApi: {
      getLesson: async () => {
        throw new Error('API should not be called.');
      },
    },
    network: { isOnline: async () => true },
    ...overrides,
  };
}

describe('GetLessonUseCase', () => {
  it('returns a complete local lesson without calling the API', async () => {
    const local = detail();
    const getLessonApi = jest.fn(async () => detail({ id: 'remote' }));
    const getLesson = createGetLessonUseCase(
      createDeps({
        loadLocalLesson: async () => local,
        catalogApi: { getLesson: getLessonApi },
      }),
    );

    await expect(getLesson({ languageId, lessonId: local.id })).resolves.toEqual(local);
    expect(getLessonApi).not.toHaveBeenCalled();
  });

  it('falls back to the API when the local graph is incomplete and the device is online', async () => {
    const remote = detail({ id: 'lesson-1' });
    const getLesson = createGetLessonUseCase(
      createDeps({
        loadLocalLesson: async () => detail({ phrases: [], exercises: [] }),
        catalogApi: { getLesson: async () => remote },
      }),
    );

    await expect(getLesson({ languageId, lessonId: 'lesson-1' })).resolves.toEqual(remote);
  });

  it('returns the local lesson when offline even if the graph is incomplete', async () => {
    const local = detail({ phrases: [], exercises: [] });
    const getLessonApi = jest.fn();
    const getLesson = createGetLessonUseCase(
      createDeps({
        loadLocalLesson: async () => local,
        catalogApi: { getLesson: getLessonApi },
        network: { isOnline: async () => false },
      }),
    );

    await expect(getLesson({ languageId, lessonId: 'lesson-1' })).resolves.toEqual(local);
    expect(getLessonApi).not.toHaveBeenCalled();
  });

  it('throws when the lesson is missing locally and the device is offline', async () => {
    const getLesson = createGetLessonUseCase(createDeps({ network: { isOnline: async () => false } }));
    await expect(getLesson({ languageId, lessonId: 'missing' })).rejects.toBeInstanceOf(LessonNotFoundError);
  });
});
