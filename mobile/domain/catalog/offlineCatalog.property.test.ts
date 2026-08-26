import * as fc from 'fast-check';

import { MAX_CATALOG_TAKE } from '@/constants/catalog';
import { createBrowseLessonCatalogUseCase } from '@/domain/catalog/browseLessonCatalogUseCase';
import type { BrowseLessonCatalogDeps, CatalogDownloadRow, CatalogPackRow, LessonCatalogApi } from '@/domain/catalog/ports';
import { LessonDownloadStatus, type CatalogLessonSummary } from '@/domain/catalog/types';
import { Level } from '@/domain/enums';
import { DownloadProgressStatus } from '@/lib/downloadProgress';

const languageId = '11111111-1111-7111-8111-111111111111';
const userId = '22222222-2222-7222-8222-222222222222';

const LEVELS = [Level.Beginner, Level.Intermediate, Level.Advanced] as const;

function toSummary(id: number, level: Level, category: string): CatalogLessonSummary {
  return {
    id: `lesson-${id}`,
    languageId,
    title: `Lesson ${id}`,
    level,
    category,
    isScenario: false,
    scenarioContext: null,
    xpReward: 10,
    updatedAt: '2026-08-23T12:00:00.000Z',
  };
}

function throwingCatalogApi(): LessonCatalogApi {
  return {
    listPublished: async () => {
      throw new Error('API must not be called while offline.');
    },
    getLesson: async () => {
      throw new Error('API must not be called while offline.');
    },
  };
}

function createOfflineDeps(
  local: CatalogLessonSummary[],
  downloadedIds: string[],
  incompleteIds: string[],
): BrowseLessonCatalogDeps {
  const packs: CatalogPackRow[] = [];
  const progress: CatalogDownloadRow[] = [];

  if (downloadedIds.length > 0) {
    packs.push({ id: 'pack-downloaded', lessonIds: downloadedIds });
    progress.push({ contentPackId: 'pack-downloaded', status: DownloadProgressStatus.Complete });
  }

  if (incompleteIds.length > 0) {
    packs.push({ id: 'pack-incomplete', lessonIds: incompleteIds });
    progress.push({ contentPackId: 'pack-incomplete', status: DownloadProgressStatus.InProgress });
  }

  return {
    listLocalLessons: async () => local,
    listProgress: async () => [],
    listContentPacks: async () => packs,
    listDownloadProgress: async () => progress,
    catalogApi: throwingCatalogApi(),
    network: { isOnline: async () => false },
  };
}

const lessonArb = fc.record({
  id: fc.integer({ min: 0, max: 31 }),
  level: fc.constantFrom(...LEVELS),
  category: fc.constantFrom('Everyday', 'Travel', 'Family', 'Work'),
  downloaded: fc.boolean(),
  incomplete: fc.boolean(),
});

describe('offline catalog reflects downloaded content', () => {
  // Feature: kalanga-language-app, Property 6: Offline Catalog Reflects Downloaded Content
  it('contains exactly the lessons in downloaded packs and marks the rest as requiring download', async () => {
    await fc.assert(
      fc.asyncProperty(
        fc.uniqueArray(lessonArb, { minLength: 0, maxLength: 8, selector: (lesson) => lesson.id }),
        async (seeds) => {
          const local = seeds.map((seed) => toSummary(seed.id, seed.level, seed.category));
          const downloadedIds = seeds.filter((seed) => seed.downloaded).map((seed) => `lesson-${seed.id}`);
          const incompleteIds = seeds
            .filter((seed) => !seed.downloaded && seed.incomplete)
            .map((seed) => `lesson-${seed.id}`);

          const browseOffline = createBrowseLessonCatalogUseCase(
            createOfflineDeps(local, downloadedIds, incompleteIds),
          );

          const offline = await browseOffline({
            languageId,
            userId,
            skip: 0,
            take: MAX_CATALOG_TAKE,
          });

          const offlineIds = offline.map((lesson) => lesson.id).sort();
          expect(offlineIds).toEqual([...downloadedIds].sort());
          expect(offline.every((lesson) => lesson.downloadStatus === LessonDownloadStatus.Downloaded)).toBe(true);

          const browseOnline = createBrowseLessonCatalogUseCase({
            ...createOfflineDeps(local, downloadedIds, incompleteIds),
            catalogApi: {
              listPublished: async () => local,
              getLesson: async () => {
                throw new Error('getLesson should not be called while browsing.');
              },
            },
            listLocalLessons: async () => [],
            network: { isOnline: async () => true },
          });

          const online = await browseOnline({
            languageId,
            userId,
            skip: 0,
            take: MAX_CATALOG_TAKE,
          });

          const downloaded = new Set(downloadedIds);
          for (const lesson of online) {
            if (downloaded.has(lesson.id)) {
              expect(lesson.downloadStatus).toBe(LessonDownloadStatus.Downloaded);
            } else {
              expect(lesson.downloadStatus).not.toBe(LessonDownloadStatus.Downloaded);
            }
          }
        },
      ),
      { numRuns: 100 },
    );
  });
});
