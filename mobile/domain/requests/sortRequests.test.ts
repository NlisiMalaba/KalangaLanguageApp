import { sortRequestsByUpvoteCount } from '@/domain/requests/sortRequests';
import { RequestStatus } from '@/domain/enums';
import type { CommunityRequest } from '@/domain/requests/types';

function request(overrides: Partial<CommunityRequest>): CommunityRequest {
  return {
    id: 'r-1',
    languageId: 'lang-1',
    submitterId: 'user-1',
    title: 'Greetings',
    description: 'Need hello',
    upvoteCount: 0,
    status: RequestStatus.Open,
    fulfilledByLessonId: null,
    createdAt: '2026-08-23T12:00:00.000Z',
    updatedAt: '2026-08-23T12:00:00.000Z',
    ...overrides,
  };
}

describe('sortRequestsByUpvoteCount', () => {
  it('orders by upvote count descending then oldest first', () => {
    const sorted = sortRequestsByUpvoteCount([
      request({ id: 'low', upvoteCount: 1, createdAt: '2026-08-24T12:00:00.000Z' }),
      request({ id: 'high-old', upvoteCount: 5, createdAt: '2026-08-20T12:00:00.000Z' }),
      request({ id: 'high-new', upvoteCount: 5, createdAt: '2026-08-23T12:00:00.000Z' }),
    ]);

    expect(sorted.map((item) => item.id)).toEqual(['high-old', 'high-new', 'low']);
  });
});
