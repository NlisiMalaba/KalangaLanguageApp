import { RequestStatus } from '@/domain/enums';
import type { CommunityRequest, RequestsApi } from '@/domain/requests/types';

const LANGUAGE_ID = '11111111-1111-7111-8111-111111111111';

export function createInMemoryRequestsApi(actorUserId: string): {
  languageId: string;
  setActor: (userId: string) => void;
  api: RequestsApi;
} {
  let actor = actorUserId;
  const requests = new Map<string, CommunityRequest>();
  const voters = new Map<string, Set<string>>();
  let nextId = 0;

  const api: RequestsApi = {
    async list(input) {
      return [...requests.values()].filter((item) => item.languageId === input.languageId);
    },
    async submit(input) {
      const id = `req-${nextId}`;
      const createdAt = `2026-08-23T12:00:${String(nextId).padStart(2, '0')}.000Z`;
      nextId += 1;
      const request: CommunityRequest = {
        id,
        languageId: input.languageId,
        submitterId: actor,
        title: input.title,
        description: input.description,
        upvoteCount: 0,
        status: RequestStatus.Open,
        fulfilledByLessonId: null,
        createdAt,
        updatedAt: createdAt,
      };
      requests.set(id, request);
      voters.set(id, new Set());
      return request;
    },
    async upvote(input) {
      const request = requests.get(input.requestId);
      if (!request || request.languageId !== input.languageId) {
        throw new Error(`Unknown request ${input.requestId}`);
      }

      const voted = voters.get(input.requestId) ?? new Set<string>();
      if (voted.has(actor)) {
        return { request, applied: false };
      }

      voted.add(actor);
      voters.set(input.requestId, voted);
      const updated: CommunityRequest = {
        ...request,
        upvoteCount: request.upvoteCount + 1,
        updatedAt: '2026-08-24T12:00:00.000Z',
      };
      requests.set(input.requestId, updated);
      return { request: updated, applied: true };
    },
    async fulfill() {
      throw new Error('fulfill is not used in these properties');
    },
  };

  return {
    languageId: LANGUAGE_ID,
    setActor: (userId: string) => {
      actor = userId;
    },
    api,
  };
}

export function voterId(index: number): string {
  return `22222222-2222-7222-8222-${String(index).padStart(12, '0')}`;
}
