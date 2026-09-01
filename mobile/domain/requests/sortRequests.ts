import type { CommunityRequest } from '@/domain/requests/types';

export function sortRequestsByUpvoteCount(requests: readonly CommunityRequest[]): CommunityRequest[] {
  return [...requests].sort((left, right) => {
    if (right.upvoteCount !== left.upvoteCount) {
      return right.upvoteCount - left.upvoteCount;
    }

    return left.createdAt.localeCompare(right.createdAt);
  });
}
