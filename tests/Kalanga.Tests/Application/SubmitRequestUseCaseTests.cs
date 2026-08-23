using Kalanga.Application.Dtos;
using Kalanga.Application.UseCases;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class SubmitRequestUseCaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Submit_creates_an_open_request_with_zero_upvotes()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, _, _, submit, _, _, _) = await SeedAsync(db);

        var created = await submit.ExecuteAsync(
            new SubmitRequestCommand(languageId, learner, "Market phrases", "Need bargaining vocabulary."));

        Assert.Equal(0, created.Request.UpvoteCount);
        Assert.Equal(RequestStatus.Open, created.Request.Status);
        Assert.Equal(learner, created.Request.SubmitterId);
        Assert.Null(created.Request.FulfilledByLessonId);
        Assert.NotEqual(default, created.Request.CreatedAt);
        Assert.NotEqual(Guid.Empty, created.Request.RequestId.Value);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Upvote_is_idempotent_per_user_and_list_is_ordered_by_count()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, contributor, _, submit, upvote, list, _) = await SeedAsync(db);
        var users = new UserRepository(db);
        var other = User.Register(languageId, $"other-{Guid.NewGuid():N}@example.com", "hash", "Other", DateTimeOffset.UtcNow);
        await users.AddAsync(languageId, other);

        var low = await submit.ExecuteAsync(
            new SubmitRequestCommand(languageId, learner, "Travel", "Airport phrases."));
        var high = await submit.ExecuteAsync(
            new SubmitRequestCommand(languageId, learner, "Greetings", "Everyday greetings."));

        var first = await upvote.ExecuteAsync(new UpvoteRequestCommand(languageId, learner, high.Request.RequestId));
        var replay = await upvote.ExecuteAsync(new UpvoteRequestCommand(languageId, learner, high.Request.RequestId));
        var secondUser = await upvote.ExecuteAsync(new UpvoteRequestCommand(languageId, other.Id, high.Request.RequestId));

        Assert.True(first.Applied);
        Assert.Equal(1, first.Request.UpvoteCount);
        Assert.False(replay.Applied);
        Assert.Equal(1, replay.Request.UpvoteCount);
        Assert.True(secondUser.Applied);
        Assert.Equal(2, secondUser.Request.UpvoteCount);

        var listed = await list.ExecuteAsync(new ListRequestsCommand(languageId, learner, Skip: 0, Take: 50));
        Assert.Equal(
            new[] { high.Request.RequestId, low.Request.RequestId },
            listed.Requests.Select(item => item.RequestId).ToArray());
        Assert.Equal(2, listed.Requests[0].UpvoteCount);
        Assert.Equal(0, listed.Requests[1].UpvoteCount);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Fulfill_links_a_lesson_notifies_the_submitter_and_blocks_further_upvotes()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, contributor, lesson, submit, upvote, list, fulfill) = await SeedAsync(db);

        var created = await submit.ExecuteAsync(
            new SubmitRequestCommand(languageId, learner, "Family", "Kinship terms."));
        var fulfilled = await fulfill.ExecuteAsync(
            new FulfillRequestCommand(languageId, contributor, created.Request.RequestId, lesson.Id));

        Assert.Equal(RequestStatus.Fulfilled, fulfilled.Request.Status);
        Assert.Equal(lesson.Id, fulfilled.Request.FulfilledByLessonId);

        var notification = Assert.Single(db.NotificationOutbox.AsEnumerable());
        Assert.Equal(RequestNotificationTypes.RequestFulfilled, notification.NotificationType);
        Assert.Equal(learner.Value, notification.RecipientUserId);
        Assert.Equal(lesson.Id.Value, notification.LessonId);

        await Assert.ThrowsAsync<InvalidRequestStateException>(() =>
            upvote.ExecuteAsync(new UpvoteRequestCommand(languageId, learner, created.Request.RequestId)));
        await Assert.ThrowsAsync<InvalidRequestStateException>(() =>
            fulfill.ExecuteAsync(new FulfillRequestCommand(languageId, contributor, created.Request.RequestId, lesson.Id)));

        var listed = await list.ExecuteAsync(new ListRequestsCommand(languageId, learner, Skip: 0, Take: 50));
        Assert.Empty(listed.Requests);

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            fulfill.ExecuteAsync(new FulfillRequestCommand(languageId, learner, created.Request.RequestId, lesson.Id)));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Suspended_users_cannot_submit_and_missing_requests_fail()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, contributor, lesson, submit, upvote, _, fulfill) = await SeedAsync(db);
        var users = new UserRepository(db);
        var actor = await users.FindByIdAsync(languageId, learner);
        Assert.NotNull(actor);
        actor.Suspend(DateTimeOffset.UtcNow);
        await users.UpdateAsync(languageId, actor);

        await Assert.ThrowsAsync<UserSuspendedException>(() =>
            submit.ExecuteAsync(new SubmitRequestCommand(languageId, learner, "Food", "Mealtime phrases.")));
        await Assert.ThrowsAsync<RequestNotFoundException>(() =>
            upvote.ExecuteAsync(new UpvoteRequestCommand(languageId, contributor, RequestId.From(Guid.CreateVersion7()))));

        await Assert.ThrowsAsync<RequestNotFoundException>(() =>
            fulfill.ExecuteAsync(
                new FulfillRequestCommand(languageId, contributor, RequestId.From(Guid.CreateVersion7()), lesson.Id)));
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            upvote.ExecuteAsync(new UpvoteRequestCommand(languageId, UserId.New(), RequestId.From(Guid.CreateVersion7()))));

        await transaction.RollbackAsync();
    }

    private static async Task<(
        LanguageId LanguageId,
        UserId Learner,
        UserId Contributor,
        Lesson Lesson,
        SubmitRequestUseCase Submit,
        UpvoteRequestUseCase Upvote,
        ListRequestsUseCase List,
        FulfillRequestUseCase Fulfill)> SeedAsync(KalangaDbContext db)
    {
        var languageId = LanguageId.New();
        db.Languages.Add(new LanguageRecord
        {
            Id = languageId.Value,
            Code = languageId.Value.ToString("N")[^10..],
            Name = "Test Language",
            Region = "Test",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var learner = User.Register(languageId, $"learner-{Guid.NewGuid():N}@example.com", "hash", "Learner", now);
        var contributor = User.Register(languageId, $"contrib-{Guid.NewGuid():N}@example.com", "hash", "Contributor", now);
        contributor.ChangeRole(Role.Contributor, now);
        var reviewer = User.Register(languageId, $"review-{Guid.NewGuid():N}@example.com", "hash", "Reviewer", now);
        reviewer.ChangeRole(Role.Reviewer, now);
        await users.AddAsync(languageId, learner);
        await users.AddAsync(languageId, contributor);
        await users.AddAsync(languageId, reviewer);

        var lessons = new LessonRepository(db);
        var lesson = Lesson.CreateDraft(languageId, contributor.Id, "Greetings", Level.Beginner, "Everyday", now);
        lesson.SubmitForReview(now);
        lesson.Approve(reviewer.Id, now);
        await lessons.AddAsync(languageId, lesson);

        var requests = new RequestRepository(db);
        var submit = new SubmitRequestUseCase(users, requests);
        var upvote = new UpvoteRequestUseCase(users, requests);
        var list = new ListRequestsUseCase(users, requests);
        var fulfill = new FulfillRequestUseCase(
            users,
            requests,
            lessons,
            new NotificationOutbox(db),
            new EfUnitOfWork(db));

        return (languageId, learner.Id, contributor.Id, lesson, submit, upvote, list, fulfill);
    }
}
