using CsCheck;
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
public sealed class LessonReviewStateMachinePropertyTests(PostgresFixture postgres)
{
    private static readonly string[] Categories = ["Everyday", "Travel", "Family", "Work"];

    // Feature: kalanga-language-app, Property 37: Lesson Review State Machine
    [Fact(Timeout = 180_000)]
    public async Task Review_actions_follow_pending_review_transitions_and_reject_illegal_ones()
    {
        var sample =
            from status in Gen.Enum<LessonStatus>()
            from action in Gen.Enum<ReviewLessonAction>()
            from level in Gen.Enum<Level>()
            from category in Gen.OneOfConst(Categories)
            from feedback in Gen.String[Gen.Char['a', 'z'], 4, 40]
            select (status, action, level, category, feedback);

        await Check.SampleAsync(
            sample,
            async (LessonStatus status, ReviewLessonAction action, Level level, string category, string feedback) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);
                var now = DateTimeOffset.UtcNow;
                var users = new UserRepository(db);
                var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now, "c");
                var reviewer = await SeedUserAsync(users, languageId, Role.Reviewer, now, "r");
                var lessons = new LessonRepository(db);
                var notifications = new CapturingNotificationOutbox();

                var lesson = CreateLesson(
                    languageId,
                    contributor,
                    reviewer,
                    $"SM-{status}-{action}",
                    level,
                    category,
                    status,
                    now);
                await lessons.AddAsync(languageId, lesson);

                var review = new ReviewLessonUseCase(users, lessons, notifications);
                var command = new ReviewLessonCommand(languageId, reviewer, lesson.Id, action, feedback);

                if (status != LessonStatus.PendingReview)
                {
                    await Assert.ThrowsAsync<InvalidLessonStatusTransitionException>(() =>
                        review.ExecuteAsync(command));
                    var unchanged = await lessons.FindByIdAsync(languageId, lesson.Id);
                    Assert.Equal(status, unchanged!.Status);
                    Assert.Empty(notifications.Enqueued);
                    await transaction.RollbackAsync();
                    return;
                }

                var result = await review.ExecuteAsync(command);
                var expected = ExpectedAfterReview(action);
                Assert.Equal(expected, result.Status);
                Assert.Equal(reviewer, result.ReviewedBy);

                var persisted = await lessons.FindByIdAsync(languageId, lesson.Id);
                Assert.NotNull(persisted);
                Assert.Equal(expected, persisted.Status);
                Assert.Equal(expected == LessonStatus.Published, persisted.IsVisibleInCatalog);

                var queue = await new ListReviewQueueUseCase(users, lessons).ExecuteAsync(
                    new ListReviewQueueCommand(languageId, reviewer, Skip: 0, Take: 50));
                Assert.DoesNotContain(queue.Lessons, item => item.LessonId == lesson.Id);

                var catalog = await new BrowseLessonCatalogUseCase(lessons).ExecuteAsync(
                    new BrowseLessonCatalogCommand(languageId, Level: null, Category: null, Skip: 0, Take: 50));

                if (expected == LessonStatus.Published)
                {
                    Assert.Contains(catalog.Lessons, item => item.LessonId == lesson.Id);
                    Assert.Null(persisted.ReviewFeedback);
                    Assert.Empty(notifications.Enqueued);
                }
                else
                {
                    Assert.DoesNotContain(catalog.Lessons, item => item.LessonId == lesson.Id);
                    Assert.Equal(feedback, persisted.ReviewFeedback);
                    var notification = Assert.Single(notifications.Enqueued);
                    Assert.Equal(contributor, notification.RecipientUserId);
                    Assert.Equal(feedback, notification.Feedback);
                    Assert.Equal(
                        action == ReviewLessonAction.Reject
                            ? ReviewNotificationTypes.LessonRejected
                            : ReviewNotificationTypes.LessonRevisionRequested,
                        notification.NotificationType);
                }

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    [Fact(Timeout = 180_000)]
    public async Task Admin_override_publishes_or_unpublishes_from_any_status()
    {
        var sample =
            from status in Gen.Enum<LessonStatus>()
            from action in Gen.Enum<AdminLessonOverrideAction>()
            from level in Gen.Enum<Level>()
            from category in Gen.OneOfConst(Categories)
            select (status, action, level, category);

        await Check.SampleAsync(
            sample,
            async (LessonStatus status, AdminLessonOverrideAction action, Level level, string category) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);
                var now = DateTimeOffset.UtcNow;
                var users = new UserRepository(db);
                var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now, "c");
                var reviewer = await SeedUserAsync(users, languageId, Role.Reviewer, now, "r");
                var admin = await SeedUserAsync(users, languageId, Role.Admin, now, "a");
                var lessons = new LessonRepository(db);

                var lesson = CreateLesson(
                    languageId,
                    contributor,
                    reviewer,
                    $"AO-{status}-{action}",
                    level,
                    category,
                    status,
                    now);
                await lessons.AddAsync(languageId, lesson);

                var result = await new OverrideLessonPublicationUseCase(users, lessons).ExecuteAsync(
                    new OverrideLessonPublicationCommand(languageId, admin, lesson.Id, action));

                var expected = action == AdminLessonOverrideAction.Publish
                    ? LessonStatus.Published
                    : LessonStatus.Unpublished;
                Assert.Equal(expected, result.Status);

                var persisted = await lessons.FindByIdAsync(languageId, lesson.Id);
                Assert.Equal(expected, persisted!.Status);
                Assert.Equal(expected == LessonStatus.Published, persisted.IsVisibleInCatalog);

                var queue = await new ListReviewQueueUseCase(users, lessons).ExecuteAsync(
                    new ListReviewQueueCommand(languageId, reviewer, Skip: 0, Take: 50));
                Assert.DoesNotContain(queue.Lessons, item => item.LessonId == lesson.Id);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static LessonStatus ExpectedAfterReview(ReviewLessonAction action) =>
        action switch
        {
            ReviewLessonAction.Approve => LessonStatus.Published,
            ReviewLessonAction.Reject => LessonStatus.Unpublished,
            ReviewLessonAction.RequestRevision => LessonStatus.Draft,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown review action."),
        };

    private static Lesson CreateLesson(
        LanguageId languageId,
        UserId contributorId,
        UserId reviewerId,
        string title,
        Level level,
        string category,
        LessonStatus status,
        DateTimeOffset now)
    {
        var lesson = Lesson.CreateDraft(languageId, contributorId, title, level, category, now);
        switch (status)
        {
            case LessonStatus.Draft:
                break;
            case LessonStatus.PendingReview:
                lesson.SubmitForReview(now);
                break;
            case LessonStatus.Published:
                lesson.SubmitForReview(now);
                lesson.Approve(reviewerId, now);
                break;
            case LessonStatus.Unpublished:
                lesson.SubmitForReview(now);
                lesson.Approve(reviewerId, now);
                lesson.Unpublish(now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown lesson status.");
        }

        return lesson;
    }

    private static async Task<UserId> SeedUserAsync(
        UserRepository users,
        LanguageId languageId,
        Role role,
        DateTimeOffset now,
        string suffix)
    {
        var user = User.Register(
            languageId,
            $"{role.ToString().ToLowerInvariant()}-{suffix}-{Guid.NewGuid():N}@example.com",
            "hash",
            role.ToString(),
            now);
        user.ChangeRole(role, now);
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
        public List<(UserId RecipientUserId, string NotificationType, string Feedback)> Enqueued { get; } = [];

        public Task EnqueueReviewFeedbackAsync(
            LanguageId languageId,
            UserId recipientUserId,
            LessonId lessonId,
            string notificationType,
            string feedback,
            CancellationToken cancellationToken = default)
        {
            Enqueued.Add((recipientUserId, notificationType, feedback));
            return Task.CompletedTask;
        }
    }
}
