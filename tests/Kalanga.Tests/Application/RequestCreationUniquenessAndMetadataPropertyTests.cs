using CsCheck;
using Kalanga.Application.Dtos;
using Kalanga.Domain.Enums;
using Kalanga.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class RequestCreationUniquenessAndMetadataPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 28: Request Creation Uniqueness and Metadata
    [Fact(Timeout = 180_000)]
    public async Task Each_submitted_request_has_a_unique_id_submitter_timestamp_and_zero_upvotes()
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
                var first = await ctx.Submit.ExecuteAsync(
                    new SubmitRequestCommand(ctx.LanguageId, ctx.Learner, title, description));
                var second = await ctx.Submit.ExecuteAsync(
                    new SubmitRequestCommand(ctx.LanguageId, ctx.Learner, title, description));

                Assert.NotEqual(first.Request.RequestId, second.Request.RequestId);
                Assert.Equal(ctx.Learner, first.Request.SubmitterId);
                Assert.Equal(ctx.Learner, second.Request.SubmitterId);
                Assert.Equal(0, first.Request.UpvoteCount);
                Assert.Equal(0, second.Request.UpvoteCount);
                Assert.Equal(RequestStatus.Open, first.Request.Status);
                Assert.Equal(title, first.Request.Title);
                Assert.Equal(description, first.Request.Description);
                Assert.True(first.Request.CreatedAt > DateTimeOffset.MinValue);
                Assert.True(second.Request.CreatedAt >= first.Request.CreatedAt);

                var stored = await db.Requests
                    .CountAsync(request => request.LanguageId == ctx.LanguageId.Value);
                Assert.Equal(2, stored);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }
}
