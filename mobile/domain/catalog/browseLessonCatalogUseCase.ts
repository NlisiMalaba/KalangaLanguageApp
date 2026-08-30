import { DEFAULT_CATALOG_SKIP, DEFAULT_CATALOG_TAKE, MAX_CATALOG_TAKE } from '@/constants/catalog';
import type { BrowseLessonCatalogDeps, CatalogFilter } from '@/domain/catalog/ports';
import {
  LessonDownloadStatus,
  type BrowseLessonCatalogInput,
  type CatalogLessonItem,
  type CatalogLessonSummary,
} from '@/domain/catalog/types';
import { requireCatalogId } from '@/domain/catalog/validation';
import type { EntityId } from '@/domain/entities';

const DOWNLOAD_COMPLETE = 'complete';

function normalizePaging(skip: number | undefined, take: number | undefined): { skip: number; take: number } {
  const resolvedSkip =
    skip === undefined || Number.isNaN(skip) || skip < DEFAULT_CATALOG_SKIP ? DEFAULT_CATALOG_SKIP : Math.floor(skip);
  const resolvedTake =
    take === undefined || Number.isNaN(take) || take < 1
      ? DEFAULT_CATALOG_TAKE
      : Math.min(MAX_CATALOG_TAKE, Math.floor(take));

  return { skip: resolvedSkip, take: resolvedTake };
}

function toFilter(input: BrowseLessonCatalogInput): CatalogFilter {
  const category = input.category?.trim() ? input.category.trim() : null;
  const { skip, take } = normalizePaging(input.skip, input.take);
  return {
    level: input.level ?? null,
    category,
    skip,
    take,
  };
}

function pageLocal<T>(items: readonly T[], skip: number, take: number): T[] {
  return items.slice(skip, skip + take);
}

function completionByLesson(rows: { lessonId: EntityId; completedAt: string | null }[]): Map<EntityId, boolean> {
  const completed = new Map<EntityId, boolean>();
  for (const row of rows) {
    completed.set(row.lessonId, row.completedAt !== null);
  }
  return completed;
}

function downloadStatusByLesson(
  packs: { id: EntityId; lessonIds: readonly EntityId[] }[],
  progress: { contentPackId: EntityId; status: string }[],
): { downloaded: Set<EntityId>; partial: Set<EntityId> } {
  const downloaded = new Set<EntityId>();
  const partial = new Set<EntityId>();
  const progressByPack = new Map<EntityId, string[]>();

  for (const row of progress) {
    const statuses = progressByPack.get(row.contentPackId) ?? [];
    statuses.push(row.status);
    progressByPack.set(row.contentPackId, statuses);
  }

  for (const pack of packs) {
    const statuses = progressByPack.get(pack.id) ?? [];
    if (statuses.length === 0) {
      continue;
    }

    const allComplete = statuses.every((status) => status === DOWNLOAD_COMPLETE);
    const target = allComplete ? downloaded : partial;
    for (const lessonId of pack.lessonIds) {
      target.add(lessonId);
    }
  }

  return { downloaded, partial };
}

function resolveDownloadStatus(
  lessonId: EntityId,
  downloaded: Set<EntityId>,
  partial: Set<EntityId>,
): (typeof LessonDownloadStatus)[keyof typeof LessonDownloadStatus] {
  if (downloaded.has(lessonId)) {
    return LessonDownloadStatus.Downloaded;
  }

  if (partial.has(lessonId)) {
    return LessonDownloadStatus.Partial;
  }

  return LessonDownloadStatus.NotDownloaded;
}

function annotate(
  lessons: CatalogLessonSummary[],
  completed: Map<EntityId, boolean>,
  downloaded: Set<EntityId>,
  partial: Set<EntityId>,
): CatalogLessonItem[] {
  return lessons.map((lesson) => ({
    ...lesson,
    isCompleted: completed.get(lesson.id) === true,
    downloadStatus: resolveDownloadStatus(lesson.id, downloaded, partial),
  }));
}

export function createBrowseLessonCatalogUseCase(deps: BrowseLessonCatalogDeps) {
  return async function browseLessonCatalog(input: BrowseLessonCatalogInput): Promise<CatalogLessonItem[]> {
    const languageId = requireCatalogId(input.languageId, 'language_id');
    const userId = requireCatalogId(input.userId, 'user_id');
    const filter = toFilter(input);

    const [progress, packs, downloadRows] = await Promise.all([
      deps.listProgress(languageId, userId),
      deps.listContentPacks(languageId),
      deps.listDownloadProgress(languageId),
    ]);
    const completed = completionByLesson(progress);
    const { downloaded, partial } = downloadStatusByLesson(packs, downloadRows);

    const local = await deps.listLocalLessons(languageId, filter);
    const online = await deps.network.isOnline();

    if (!online) {
      const offline = local.filter((lesson) => downloaded.has(lesson.id));
      return annotate(pageLocal(offline, filter.skip, filter.take), completed, downloaded, partial);
    }

    if (local.length > 0) {
      return annotate(pageLocal(local, filter.skip, filter.take), completed, downloaded, partial);
    }

    const remote = await deps.catalogApi.listPublished(languageId, filter);
    return annotate(remote, completed, downloaded, partial);
  };
}
