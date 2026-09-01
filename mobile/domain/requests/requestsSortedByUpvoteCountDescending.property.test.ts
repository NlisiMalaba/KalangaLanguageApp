import * as fc from 'fast-check';

import { createInMemoryRequestsApi, voterId } from '@/domain/requests/requestTestHarness';
import { createListRequestsUseCase, createSubmitRequestUseCase, createUpvoteRequestUseCase } from '@/domain/requests/useCases';

const votesPerRequestArb = fc.array(fc.integer({ min: 0, max: 4 }), { minLength: 2, maxLength: 5 });

describe('requests sorted by upvote count descending', () => {
  // Feature: kalanga-language-app, Property 30: Requests Sorted by Upvote Count Descending
  it('lists open requests by upvote count descending then created-at ascending', async () => {
    await fc.assert(
      fc.asyncProperty(votesPerRequestArb, async (votesPerRequest) => {
        const harness = createInMemoryRequestsApi(voterId(0));
        const submit = createSubmitRequestUseCase(harness.api);
        const upvote = createUpvoteRequestUseCase(harness.api);
        const list = createListRequestsUseCase(harness.api);

        for (let i = 0; i < votesPerRequest.length; i += 1) {
          harness.setActor(voterId(0));
          const created = await submit(
            harness.languageId,
            `Topic${String(i).padStart(2, '0')}`,
            `Need more phrases for topic ${i}.`,
          );

          for (let v = 0; v < votesPerRequest[i]; v += 1) {
            harness.setActor(voterId(v));
            const voted = await upvote(harness.languageId, created.id);
            expect(voted.applied).toBe(true);
          }
        }

        const listed = await list(harness.languageId);
        expect(listed).toHaveLength(votesPerRequest.length);

        const expectedIds = [...listed]
          .sort((left, right) => {
            if (right.upvoteCount !== left.upvoteCount) {
              return right.upvoteCount - left.upvoteCount;
            }

            return left.createdAt.localeCompare(right.createdAt);
          })
          .map((item) => item.id);
        expect(listed.map((item) => item.id)).toEqual(expectedIds);
        expect(listed.map((item) => item.upvoteCount)).toEqual(
          [...votesPerRequest].sort((left, right) => right - left),
        );
      }),
      { numRuns: 100 },
    );
  });
});
