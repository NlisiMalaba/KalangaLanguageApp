import * as fc from 'fast-check';

import { createInMemoryRequestsApi, voterId } from '@/domain/requests/requestTestHarness';
import { createSubmitRequestUseCase, createUpvoteRequestUseCase } from '@/domain/requests/useCases';

const extraAttemptsArb = fc.integer({ min: 1, max: 6 });
const otherVotersArb = fc.integer({ min: 0, max: 4 });

describe('upvote idempotence per user per request', () => {
  // Feature: kalanga-language-app, Property 29: Upvote Idempotence Per User Per Request
  it('does not increase the count when the same user upvotes again', async () => {
    await fc.assert(
      fc.asyncProperty(extraAttemptsArb, otherVotersArb, async (extraAttempts, otherVoters) => {
        const learner = voterId(0);
        const harness = createInMemoryRequestsApi(learner);
        const submit = createSubmitRequestUseCase(harness.api);
        const upvote = createUpvoteRequestUseCase(harness.api);

        const created = await submit(harness.languageId, 'Greetings', 'Need everyday greetings.');
        const first = await upvote(harness.languageId, created.id);
        expect(first.applied).toBe(true);
        expect(first.request.upvoteCount).toBe(1);

        for (let i = 0; i < extraAttempts; i += 1) {
          const replay = await upvote(harness.languageId, created.id);
          expect(replay.applied).toBe(false);
          expect(replay.request.upvoteCount).toBe(1);
        }

        let expected = 1;
        for (let i = 1; i <= otherVoters; i += 1) {
          harness.setActor(voterId(i));
          const voted = await upvote(harness.languageId, created.id);
          expect(voted.applied).toBe(true);
          expected += 1;
          expect(voted.request.upvoteCount).toBe(expected);
        }
      }),
      { numRuns: 100 },
    );
  });
});
