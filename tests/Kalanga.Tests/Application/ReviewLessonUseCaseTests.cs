using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.Out;
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
public sealed class ReviewLessonUseCaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Approve_publishes_the_lesson_and_it_appears_in_the_catalog()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, contributor, reviewer, lessons, users, notifications) = await SeedAsync(db);
        var pending = await CreatePendingAsync(users, lessons, languageId, contributor);

        var result = await new ReviewLessonUseCase(users, lessons, notifications).ExecuteAsync(
            new ReviewLessonCommand(
                languageId,
                reviewer,
                pending.LessonId,
                ReviewLessonAction.Approve,
                Feedback: null));

        Assert.Equal(LessonStatus.Published, result.Status);
        Assert.Equal(reviewer, result.ReviewedBy);

        var catalog = await new BrowseLessonCatalogUseCase(lessons).ExecuteAsync(
            new BrowseLessonCatalogCommand(languageId, Level: null, Category: null, Skip: 0, Take: 50));
        Assert.Contains(catalog.Lessons, item => item.LessonId == pending.LessonId);

        var queue = await new ListReviewQueueUseCase(users, lessons).ExecuteAsync(
            new ListReviewQueueCommand(languageId, reviewer, Skip: 0, Take: 50));
        Assert.DoesNotContain(queue.Lessons, item => item.LessonId == pending.LessonId);

        Assert.Empty(notifications.Enqueued);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Reject_unpublishes_records_feedback_and_enqueues_contributor_notification()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, contributor, reviewer, lessons, users, notifications) = await SeedAsync(db);
        var pending = await CreatePendingAsync(users, lessons, languageId, contributor);

        var result = await new ReviewLessonUseCase(users, lessons, notifications).ExecuteAsync(
            new ReviewLessonCommand(
                languageId,
                reviewer,
                pending.LessonId,
                ReviewLessonAction.Reject,
                "Inaccurate translation."));

        Assert.Equal(LessonStatus.Unpublished, result.Status);
        Assert.Equal("Inaccurate translation.", result.ReviewFeedback);

        var catalog = await new BrowseLessonCatalogUseCase(lessons).ExecuteAsync(
            new BrowseLessonCatalogCommand(languageId, Level: null, Category: null, Skip: 0, Take: 50));
        Assert.DoesNotContain(catalog.Lessons, item => item.LessonId == pending.LessonId);

        var notification = Assert.Single(notifications.Enqueued);
        Assert.Equal(contributor, notification.RecipientUserId);
        Assert.Equal(ReviewNotificationTypes.LessonRejected, notification.NotificationType);
        Assert.Equal("Inaccurate translation.", notification.Feedback);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Request_revision_returns_the_lesson_to_draft_with_feedback()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, contributor, reviewer, lessons, users, notifications) = await SeedAsync(db);
        var pending = await CreatePendingAsync(users, lessons, languageId, contributor);

        var result = await new ReviewLessonUseCase(users, lessons, notifications).ExecuteAsync(
            new ReviewLessonCommand(
                languageId,
                reviewer,
                pending.LessonId,
                ReviewLessonAction.RequestRevision,
                "Add a formal variation."));

        Assert.Equal(LessonStatus.Draft, result.Status);
        Assert.Equal("Add a formal variation.", result.ReviewFeedback);

        var queue = await lessons.FindPendingReviewAsync(languageId, skip: 0, take: 50);
        Assert.DoesNotContain(queue, lesson => lesson.Id == pending.LessonId);

        var notification = Assert.Single(notifications.Enqueued);
        Assert.Equal(ReviewNotificationTypes.LessonRevisionRequested, notification.NotificationType);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Admin_can_publish_and_unpublish_without_the_review_queue()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var admin = await SeedUserAsync(users, languageId, Role.Admin, now);
        var lessons = new LessonRepository(db);

        var created = await new CreateLessonUseCase(users, lessons).ExecuteAsync(
            new CreateLessonCommand(languageId, admin, "Admin override", Level.Beginner, "Everyday"));

        var overridePublication = new OverrideLessonPublicationUseCase(users, lessons);
        var published = await overridePublication.ExecuteAsync(
            new OverrideLessonPublicationCommand(
                languageId,
                admin,
                created.LessonId,
                AdminLessonOverrideAction.Publish));
        Assert.Equal(LessonStatus.Published, published.Status);

        var unpublished = await overridePublication.ExecuteAsync(
            new OverrideLessonPublicationCommand(
                languageId,
                admin,
                created.LessonId,
                AdminLessonOverrideAction.Unpublish));
        Assert.Equal(LessonStatus.Unpublished, unpublished.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Contributor_cannot_list_the_review_queue()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var users = new UserRepository(db);
        var contributor = await SeedUserAsync(users, languageId, Role.Contributor, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            new ListReviewQueueUseCase(users, new LessonRepository(db)).ExecuteAsync(
                new ListReviewQueueCommand(languageId, contributor, Skip: 0, Take: 50)));

        await transaction.RollbackAsync();
    }

    private static async Task<(LanguageId LanguageId, UserId Contributor, UserId Reviewer, LessonRepository Lessons, UserRepository Users, CapturingNotificationOutbox Notifications)> SeedAsync(
        KalangaDbContext db)
    {
        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now, "c");
        var reviewer = await SeedUserAsync(users, languageId, Role.Reviewer, now, "r");
        return (languageId, contributor, reviewer, new LessonRepository(db), users, new CapturingNotificationOutbox());
    }

    private static async Task<CreateLessonResult> CreatePendingAsync(
        UserRepository users,
        LessonRepository lessons,
        LanguageId languageId,
        UserId contributor)
    {
        var created = await new CreateLessonUseCase(users, lessons).ExecuteAsync(
            new CreateLessonCommand(languageId, contributor, "Greetings", Level.Beginner, "Everyday"));
        await new SubmitLessonForReviewUseCase(users, lessons).ExecuteAsync(
            new SubmitLessonForReviewCommand(languageId, contributor, created.LessonId));
        return created;
    }

    private static async Task<UserId> SeedUserAsync(
        UserRepository users,
        LanguageId languageId,
        Role role,
        DateTimeOffset now,
        string? suffix = null)
    {
        var user = User.Register(
            languageId,
            $"{role.ToString().ToLowerInvariant()}-{suffix}-{Guid.NewGuid():N}@example.com",
            "hash",
            role.ToString(),
            now);
        if (role != Role.Learner)
        {
            user.ChangeRole(role, now);
        }

        await users.AddAsync(languageId, user);
        return user.Id;
    }

    private static async Task SeedLanguageAsync(KalangaDbContext db, LanguageId languageId)
    {
        db.Languages.Add(new LanguageRecord
        {
            Id = languageId.Value,
            Code = languageId.Value.ToString("N")[..10],
            Name = "Test Language",
            Region = "Test",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private sealed class CapturingNotificationOutbox : INotificationOutbox
    {
        public List<(UserId RecipientUserId, LessonId LessonId, string NotificationType, string Feedback)> Enqueued { get; } = [];

        public Task EnqueueReviewFeedbackAsync(
            LanguageId languageId,
            UserId recipientUserId,
            LessonId lessonId,
            string notificationType,
            string feedback,
            CancellationToken cancellationToken = default)
        {
            Enqueued.Add((recipientUserId, lessonId, notificationType, feedback));
            return Task.CompletedTask;
        }
    }
}
