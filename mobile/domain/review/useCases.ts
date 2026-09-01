import { ReviewError } from '@/domain/review/errors';
import type { ReviewApi } from '@/domain/review/types';

export function createListReviewQueueUseCase(api: ReviewApi) {
  return async function listReviewQueue(languageId: string) {
    return api.listQueue(languageId);
  };
}

export function createApproveLessonUseCase(api: ReviewApi) {
  return async function approveLesson(languageId: string, lessonId: string) {
    if (!lessonId.trim()) {
      throw new ReviewError('A lesson is required to approve.');
    }

    return api.approve(languageId, lessonId);
  };
}
