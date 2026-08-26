import { createBrowseLessonCatalogUseCase } from '@/domain/catalog/browseLessonCatalogUseCase';
import { CatalogValidationError } from '@/domain/catalog/errors';
import type { BrowseLessonCatalogDeps, LessonCatalogApi } from '@/domain/catalog/ports';
import { LessonDownloadStatus, type CatalogLessonSummary } from '@/domain/catalog/types';
import { Level } from '@/domain/enums';
import { DownloadProgressStatus } from '@/lib/downloadProgress';

const languageId = '11111111-1111-7111-8111-111111111111';
const userId = '22222222-2222-7222-8222-222222222222';

function summary(overrides: Partial<CatalogLessonSummary> = {}): CatalogLessonSummary {
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
    ...overrides,
  };
}

function unusedGetLesson(): LessonCatalogApi['getLesson'] {
  return async () => {
    throw new Error('getLesson should not be called while browsing.');
  };
}

function createDeps(overrides: Partial<BrowseLessonCatalogDeps> = {}): BrowseLessonCatalogDeps {
  const catalogApi: LessonCatalogApi = {
    listPublished: jest.fn(async () => []),
    getLesson: unusedGetLesson(),
  };

  return {
    listLocalLessons: jest.fn(async () => []),
    listProgress: jest.fn(async () => []),
    listContentPacks: jest.fn(async () => []),
    listDownloadProgress: jest.fn(async () => []),
    catalogApi,
    network: { isOnline: async () => true },
    ...overrides,
  };
}

describe('BrowseLessonCatalogUseCase', () => {
  it('reads sqlite first and does not call the API when local lessons exist', async () => {
    const local = summary();
    const catalogApi: LessonCatalogApi = {
      listPublished: jest.fn(async () => [summary({ id: 'remote-1' })]),
      getLesson: unusedGetLesson(),
    };
    const browse = createBrowseLessonCatalogUseCase(
      createDeps({
        listLocalLessons: jest.fn(async () => [local]),
        listProgress: jest.fn(async () => [{ lessonId: local.id, completedAt: '2026-08-23T12:00:00.000Z' }]),
        catalogApi,
        network: { isOnline: async () => true },
      }),
    );

    const result = await browse({ languageId, userId, level: Level.Beginner, category: 'Everyday' });

    expect(catalogApi.listPublished).not.toHaveBeenCalled();
    expect(result).toHaveLength(1);
    expect(result[0]?.id).toBe('lesson-1');
    expect(result[0]?.isCompleted).toBe(true);
    expect(result[0]?.downloadStatus).toBe(LessonDownloadStatus.NotDownloaded);
    expect(result[0]?.level).toBe(Level.Beginner);
    expect(result[0]?.category).toBe('Everyday');
  });

  it('falls back to the API when sqlite is empty and the device is online', async () => {
    const remote = summary({ id: 'remote-1' });
    const catalogApi: LessonCatalogApi = { listPublished: jest.fn(async () => [remote]), getLesson: unusedGetLesson() };
    const browse = createBrowseLessonCatalogUseCase(
      createDeps({
        catalogApi,
        listProgress: jest.fn(async () => [{ lessonId: 'remote-1', completedAt: null }]),
        listContentPacks: jest.fn(async () => [{ id: 'pack-1', lessonIds: ['remote-1'] }]),
        listDownloadProgress: jest.fn(async () => [
          { contentPackId: 'pack-1', status: DownloadProgressStatus.Complete },
        ]),
      }),
    );

    const result = await browse({ languageId, userId });

    expect(catalogApi.listPublished).toHaveBeenCalled();
    expect(result[0]?.isCompleted).toBe(false);
    expect(result[0]?.downloadStatus).toBe(LessonDownloadStatus.Downloaded);
  });

  it('annotates a pack in progress as partial', async () => {
    const remote = summary({ id: 'remote-1' });
    const browse = createBrowseLessonCatalogUseCase(
      createDeps({
        catalogApi: { listPublished: async () => [remote], getLesson: unusedGetLesson() },
        listContentPacks: async () => [{ id: 'pack-1', lessonIds: ['remote-1'] }],
        listDownloadProgress: async () => [
          { contentPackId: 'pack-1', status: DownloadProgressStatus.InProgress },
        ],
      }),
    );

    const result = await browse({ languageId, userId });
    expect(result[0]?.downloadStatus).toBe(LessonDownloadStatus.Partial);
  });

  it('returns an empty catalog when sqlite is empty and the device is offline', async () => {
    const catalogApi: LessonCatalogApi = { listPublished: jest.fn(async () => [summary()]), getLesson: unusedGetLesson() };
    const browse = createBrowseLessonCatalogUseCase(
      createDeps({
        catalogApi,
        network: { isOnline: async () => false },
      }),
    );

    await expect(browse({ languageId, userId })).resolves.toEqual([]);
    expect(catalogApi.listPublished).not.toHaveBeenCalled();
  });

  it('offline catalog excludes local lessons that are not in a downloaded pack', async () => {
    const catalogApi: LessonCatalogApi = {
      listPublished: jest.fn(async () => [summary({ id: 'remote-1' })]),
      getLesson: unusedGetLesson(),
    };
    const browse = createBrowseLessonCatalogUseCase(
      createDeps({
        listLocalLessons: async () => [summary({ id: 'on-device' }), summary({ id: 'downloaded' })],
        listContentPacks: async () => [{ id: 'pack-1', lessonIds: ['downloaded'] }],
        listDownloadProgress: async () => [
          { contentPackId: 'pack-1', status: DownloadProgressStatus.Complete },
        ],
        catalogApi,
        network: { isOnline: async () => false },
      }),
    );

    const result = await browse({ languageId, userId });
    expect(catalogApi.listPublished).not.toHaveBeenCalled();
    expect(result.map((lesson) => lesson.id)).toEqual(['downloaded']);
    expect(result[0]?.downloadStatus).toBe(LessonDownloadStatus.Downloaded);
  });

  it('rejects a missing tenant or user', async () => {
    const browse = createBrowseLessonCatalogUseCase(createDeps());
    await expect(browse({ languageId: '  ', userId })).rejects.toBeInstanceOf(CatalogValidationError);
    await expect(browse({ languageId, userId: '' })).rejects.toBeInstanceOf(CatalogValidationError);
  });
});
