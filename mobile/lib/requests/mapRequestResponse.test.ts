import { RequestApiError } from '@/domain/requests/errors';
import { RequestStatus } from '@/domain/enums';
import { mapListRequestsResponse, mapUpvoteRequestResponse } from '@/lib/requests/mapRequestResponse';

describe('mapListRequestsResponse', () => {
  it('unwraps nested ids and status', () => {
    const items = mapListRequestsResponse({
      requests: [
        {
          requestId: { value: 'req-1' },
          languageId: { value: 'lang-1' },
          submitterId: { value: 'user-1' },
          title: 'Market',
          description: 'Bargaining phrases',
          upvoteCount: 3,
          status: 'Open',
          fulfilledByLessonId: null,
          createdAt: '2026-08-23T12:00:00Z',
          updatedAt: '2026-08-23T12:00:00Z',
        },
      ],
    });

    expect(items[0]).toMatchObject({
      id: 'req-1',
      languageId: 'lang-1',
      upvoteCount: 3,
      status: RequestStatus.Open,
      fulfilledByLessonId: null,
    });
  });

  it('rejects a payload without requests', () => {
    expect(() => mapListRequestsResponse({})).toThrow(RequestApiError);
  });
});

describe('mapUpvoteRequestResponse', () => {
  it('maps applied and nested request', () => {
    const result = mapUpvoteRequestResponse({
      applied: true,
      request: {
        requestId: 'req-1',
        languageId: 'lang-1',
        submitterId: 'user-1',
        title: 'Market',
        description: 'Bargaining phrases',
        upvoteCount: 1,
        status: 'Open',
        createdAt: '2026-08-23T12:00:00Z',
        updatedAt: '2026-08-23T12:00:00Z',
      },
    });

    expect(result.applied).toBe(true);
    expect(result.request.upvoteCount).toBe(1);
  });
});
