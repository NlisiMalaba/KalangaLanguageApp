import { MAX_REQUEST_TITLE_LENGTH } from '@/constants/requests';
import { RequestValidationError } from '@/domain/requests/errors';

export function assertRequestDraftValid(title: string, description: string): void {
  if (title.trim().length === 0) {
    throw new RequestValidationError('A request title is required.');
  }

  if (title.trim().length > MAX_REQUEST_TITLE_LENGTH) {
    throw new RequestValidationError(`Title must be ${MAX_REQUEST_TITLE_LENGTH} characters or fewer.`);
  }

  if (description.trim().length === 0) {
    throw new RequestValidationError('Describe the phrases or situation you need.');
  }
}

export function assertRequestId(requestId: string): void {
  if (requestId.trim().length === 0) {
    throw new RequestValidationError('A request is required.');
  }
}

export function assertLessonId(lessonId: string): void {
  if (lessonId.trim().length === 0) {
    throw new RequestValidationError('Choose a lesson to link to this request.');
  }
}
