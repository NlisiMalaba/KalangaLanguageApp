import { LessonNotFoundError } from '@/domain/catalog/errors';
import type { GetLessonDeps } from '@/domain/catalog/ports';
import { isPublishedLessonStructurallyComplete } from '@/domain/catalog/structuralCompleteness';
import { requireCatalogId } from '@/domain/catalog/validation';
import type { EntityId } from '@/domain/entities';

export type GetLessonInput = {
  languageId: EntityId;
  lessonId: EntityId;
};

export function createGetLessonUseCase(deps: GetLessonDeps) {
  return async function getLesson(input: GetLessonInput) {
    const languageId = requireCatalogId(input.languageId, 'language_id');
    const lessonId = requireCatalogId(input.lessonId, 'lesson_id');

    const local = await deps.loadLocalLesson(languageId, lessonId);
    if (local && isPublishedLessonStructurallyComplete(local)) {
      return local;
    }

    if (await deps.network.isOnline()) {
      try {
        return await deps.catalogApi.getLesson(languageId, lessonId);
      } catch (error) {
        if (local) {
          return local;
        }

        throw error;
      }
    }

    if (local) {
      return local;
    }

    throw new LessonNotFoundError(lessonId);
  };
}
