import { RequestValidationError } from '@/domain/requests/errors';
import { RequestStatus } from '@/domain/enums';
import type { CommunityRequest, RequestsApi } from '@/domain/requests/types';
import {
  createFulfillRequestUseCase,
  createListRequestsUseCase,
  createSubmitRequestUseCase,
  createUpvoteRequestUseCase,
} from '@/domain/requests/useCases';

const languageId = 'lang-1';

function request(overrides: Partial<CommunityRequest> = {}): CommunityRequest {
  return {
    id: 'req-1',
    languageId,
    submitterId: 'user-1',
    title: 'Market',
    description: 'Bargaining',
    upvoteCount: 2,
    status: RequestStatus.Open,
    fulfilledByLessonId: null,
    createdAt: '2026-08-23T12:00:00.000Z',
    updatedAt: '2026-08-23T12:00:00.000Z',
    ...overrides,
  };
}

function api(overrides: Partial<RequestsApi> = {}): RequestsApi {
  return {
    list: jest.fn(async () => [request({ id: 'low', upvoteCount: 0 }), request({ id: 'high', upvoteCount: 3 })]),
    submit: jest.fn(async () => request()),
    upvote: jest.fn(async () => ({ request: request({ upvoteCount: 3 }), applied: true })),
    fulfill: jest.fn(async () => request({ status: RequestStatus.Fulfilled, fulfilledByLessonId: 'lesson-1' })),
    ...overrides,
  };
}

describe('request use cases', () => {
  it('lists requests sorted by upvotes and scoped to the tenant', async () => {
    const port = api({
      list: jest.fn(async () => [
        request({ id: 'other', languageId: 'other-lang', upvoteCount: 99 }),
        request({ id: 'low', upvoteCount: 1 }),
        request({ id: 'high', upvoteCount: 4 }),
      ]),
    });
    const listed = await createListRequestsUseCase(port)(languageId);
    expect(listed.map((item) => item.id)).toEqual(['high', 'low']);
  });

  it('rejects an empty request title', async () => {
    const submit = createSubmitRequestUseCase(api());
    await expect(submit(languageId, '  ', 'Need greetings')).rejects.toBeInstanceOf(RequestValidationError);
  });

  it('submits a trimmed request', async () => {
    const port = api();
    await createSubmitRequestUseCase(port)(languageId, '  Market  ', '  Bargaining  ');
    expect(port.submit).toHaveBeenCalledWith({
      languageId,
      title: 'Market',
      description: 'Bargaining',
    });
  });

  it('upvotes by request id', async () => {
    const port = api();
    const result = await createUpvoteRequestUseCase(port)(languageId, 'req-1');
    expect(result.applied).toBe(true);
    expect(port.upvote).toHaveBeenCalledWith({ languageId, requestId: 'req-1' });
  });

  it('fulfills with a lesson id', async () => {
    const port = api();
    await createFulfillRequestUseCase(port)(languageId, 'req-1', 'lesson-1');
    expect(port.fulfill).toHaveBeenCalledWith({ languageId, requestId: 'req-1', lessonId: 'lesson-1' });
  });
});
