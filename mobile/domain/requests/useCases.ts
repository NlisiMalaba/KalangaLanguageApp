import { DEFAULT_REQUESTS_TAKE } from '@/constants/requests';
import { sortRequestsByUpvoteCount } from '@/domain/requests/sortRequests';
import type { RequestsApi } from '@/domain/requests/types';
import { assertLessonId, assertRequestDraftValid, assertRequestId } from '@/domain/requests/validateRequest';

export function createListRequestsUseCase(api: RequestsApi) {
  return async function listRequests(languageId: string) {
    const items = await api.list({ languageId, skip: 0, take: DEFAULT_REQUESTS_TAKE });
    return sortRequestsByUpvoteCount(items.filter((item) => item.languageId === languageId));
  };
}

export function createSubmitRequestUseCase(api: RequestsApi) {
  return async function submitRequest(languageId: string, title: string, description: string) {
    assertRequestDraftValid(title, description);
    return api.submit({
      languageId,
      title: title.trim(),
      description: description.trim(),
    });
  };
}

export function createUpvoteRequestUseCase(api: RequestsApi) {
  return async function upvoteRequest(languageId: string, requestId: string) {
    assertRequestId(requestId);
    return api.upvote({ languageId, requestId });
  };
}

export function createFulfillRequestUseCase(api: RequestsApi) {
  return async function fulfillRequest(languageId: string, requestId: string, lessonId: string) {
    assertRequestId(requestId);
    assertLessonId(lessonId);
    return api.fulfill({ languageId, requestId, lessonId });
  };
}
