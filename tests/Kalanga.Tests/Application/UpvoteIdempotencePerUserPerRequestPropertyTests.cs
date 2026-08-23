using CsCheck;
using Kalanga.Application.Dtos;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class UpvoteIdempotencePerUserPerRequestPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 29: Upvote Idempotence Per User Per Request
    [Fact(Timeout = 180_000)]
    public async Task Repeating_an_upvote_from_the_same_user_does_not_increase_the_count()
    {
        var input =
            from extraAttempts in Gen.Int[1, 6]
            from otherVoters in Gen.Int[0, 4]
            select (extraAttempts, otherVoters);

        await Check.SampleAsync(
            input,
            async (int extraAttempts, int otherVoters) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var ctx = await RequestTestSeed.CreateAsync(db, extraVoters: otherVoters);
                var created = await ctx.Submit.ExecuteAsync(
                    new SubmitRequestCommand(ctx.LanguageId, ctx.Learner, "Greetings", "Need everyday greetings."));

                var first = await ctx.Upvote.ExecuteAsync(
                    new UpvoteRequestCommand(ctx.LanguageId, ctx.Learner, created.Request.RequestId));
                Assert.True(first.Applied);
                Assert.Equal(1, first.Request.UpvoteCount);

                for (var i = 0; i < extraAttempts; i++)
                {
                    var replay = await ctx.Upvote.ExecuteAsync(
                        new UpvoteRequestCommand(ctx.LanguageId, ctx.Learner, created.Request.RequestId));
                    Assert.False(replay.Applied);
                    Assert.Equal(1, replay.Request.UpvoteCount);
                }

                var expected = 1;
                foreach (var voter in ctx.Voters.Skip(1))
                {
                    var voted = await ctx.Upvote.ExecuteAsync(
                        new UpvoteRequestCommand(ctx.LanguageId, voter, created.Request.RequestId));
                    Assert.True(voted.Applied);
                    expected++;
                    Assert.Equal(expected, voted.Request.UpvoteCount);
                }

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }
}
