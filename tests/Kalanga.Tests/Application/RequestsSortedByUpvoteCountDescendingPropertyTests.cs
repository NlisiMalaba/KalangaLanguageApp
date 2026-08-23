using CsCheck;
using Kalanga.Application.Dtos;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class RequestsSortedByUpvoteCountDescendingPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 30: Requests Sorted by Upvote Count Descending
    [Fact(Timeout = 180_000)]
    public async Task Open_requests_are_listed_by_upvote_count_descending()
    {
        var input = Gen.Int[0, 4].Array[2, 5];

        await Check.SampleAsync(
            input,
            async (int[] votesPerRequest) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var ctx = await RequestTestSeed.CreateAsync(db, extraVoters: 4);
                for (var i = 0; i < votesPerRequest.Length; i++)
                {
                    var created = await ctx.Submit.ExecuteAsync(
                        new SubmitRequestCommand(
                            ctx.LanguageId,
                            ctx.Learner,
                            $"Topic{i:D2}",
                            $"Need more phrases for topic {i}."));

                    for (var v = 0; v < votesPerRequest[i]; v++)
                    {
                        var voted = await ctx.Upvote.ExecuteAsync(
                            new UpvoteRequestCommand(ctx.LanguageId, ctx.Voters[v], created.Request.RequestId));
                        Assert.True(voted.Applied);
                    }
                }

                var listed = await ctx.List.ExecuteAsync(
                    new ListRequestsCommand(ctx.LanguageId, ctx.Learner, Skip: 0, Take: 50));

                Assert.Equal(votesPerRequest.Length, listed.Requests.Count);
                var expected = listed.Requests
                    .OrderByDescending(item => item.UpvoteCount)
                    .ThenBy(item => item.CreatedAt)
                    .Select(item => item.RequestId)
                    .ToArray();
                Assert.Equal(expected, listed.Requests.Select(item => item.RequestId).ToArray());
                Assert.Equal(
                    votesPerRequest.OrderByDescending(count => count).ToArray(),
                    listed.Requests.Select(item => item.UpvoteCount).ToArray());

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }
}
