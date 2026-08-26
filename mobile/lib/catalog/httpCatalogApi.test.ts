import { CatalogApiError } from '@/domain/catalog/errors';
import { Level } from '@/domain/enums';
import { mapBrowseLessonCatalogResponse } from '@/lib/catalog/httpCatalogApi';

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
