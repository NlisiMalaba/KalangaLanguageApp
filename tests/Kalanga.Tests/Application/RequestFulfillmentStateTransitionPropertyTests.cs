using CsCheck;
using Kalanga.Application.Dtos;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class RequestFulfillmentStateTransitionPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 31: Request Fulfillment State Transition
    [Fact(Timeout = 180_000)]
    public async Task Fulfilling_an_open_request_links_the_lesson_and_blocks_further_changes()
    {
        var input =
            from title in Gen.String[Gen.Char['a', 'z'], 3, 40]
            from description in Gen.String[Gen.Char.AlphaNumeric, 8, 80]
            select (title, description);

        await Check.SampleAsync(
            input,
            async (string title, string description) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var ctx = await RequestTestSeed.CreateAsync(db);
                var created = await ctx.Submit.ExecuteAsync(
                    new SubmitRequestCommand(ctx.LanguageId, ctx.Learner, title, description));

                var fulfilled = await ctx.Fulfill.ExecuteAsync(
                    new FulfillRequestCommand(
                        ctx.LanguageId,
                        ctx.Contributor,
                        created.Request.RequestId,
                        ctx.Lesson.Id));

                Assert.Equal(RequestStatus.Fulfilled, fulfilled.Request.Status);
                Assert.Equal(ctx.Lesson.Id, fulfilled.Request.FulfilledByLessonId);
                Assert.Equal(created.Request.RequestId, fulfilled.Request.RequestId);

                await Assert.ThrowsAsync<InvalidRequestStateException>(() =>
                    ctx.Fulfill.ExecuteAsync(
                        new FulfillRequestCommand(
                            ctx.LanguageId,
                            ctx.Contributor,
                            created.Request.RequestId,
                            ctx.Lesson.Id)));
                await Assert.ThrowsAsync<InvalidRequestStateException>(() =>
                    ctx.Upvote.ExecuteAsync(
                        new UpvoteRequestCommand(ctx.LanguageId, ctx.Learner, created.Request.RequestId)));

                var listed = await ctx.List.ExecuteAsync(
                    new ListRequestsCommand(ctx.LanguageId, ctx.Learner, Skip: 0, Take: 50));
                Assert.DoesNotContain(listed.Requests, item => item.RequestId == created.Request.RequestId);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }
}
