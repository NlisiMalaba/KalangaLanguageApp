using CsCheck;
using Kalanga.Application.Dtos;
using Kalanga.Application.UseCases;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class ReviewQueueContainsPendingLessonsPropertyTests(PostgresFixture postgres)
{
    private static readonly string[] Categories = ["Everyday", "Travel", "Family", "Work"];

    // Feature: kalanga-language-app, Property 36: Review Queue Contains All Pending Lessons
    [Fact(Timeout = 180_000)]
    public async Task Review_queue_contains_exactly_the_pending_review_lessons_for_the_tenant()
    {
        var lessonSpec =
            from otherTenant in Gen.Bool
            from status in Gen.Enum<LessonStatus>()
            from level in Gen.Enum<Level>()
            from category in Gen.OneOfConst(Categories)
            select (otherTenant, status, level, category);

        await Check.SampleAsync(
            lessonSpec.Array[0, 8],
            async ((bool OtherTenant, LessonStatus Status, Level Level, string Category)[] specs) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                var otherLanguageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);
                await SeedLanguageAsync(db, otherLanguageId);

                var now = DateTimeOffset.UtcNow;
                var users = new UserRepository(db);
                var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now, "a");
                var otherContributor = await SeedUserAsync(users, otherLanguageId, Role.Contributor, now, "b");
                var reviewer = await SeedUserAsync(users, languageId, Role.Reviewer, now, "r");
                var otherReviewer = await SeedUserAsync(users, otherLanguageId, Role.Reviewer, now, "or");

                var lessons = new LessonRepository(db);
                var seeded = new List<Lesson>(specs.Length);

                for (var i = 0; i < specs.Length; i++)
                {
                    var spec = specs[i];
                    var tenant = spec.OtherTenant ? otherLanguageId : languageId;
                    var owner = spec.OtherTenant ? otherContributor : contributor;
                    var approvedBy = spec.OtherTenant ? otherReviewer : reviewer;
                    var lesson = CreateLesson(
                        tenant,
                        owner,
                        approvedBy,
                        $"Q{i:D2}-{spec.Status}-{spec.Level}-{spec.Category}",
                        spec.Level,
                        spec.Category,
                        spec.Status,
                        now.AddMinutes(i));
                    await lessons.AddAsync(tenant, lesson);
                    seeded.Add(lesson);
                }

                var expectedIds = seeded
                    .Where(lesson => lesson.LanguageId == languageId && lesson.Status == LessonStatus.PendingReview)
                    .Select(lesson => lesson.Id)
                    .ToHashSet();

                var queue = await new ListReviewQueueUseCase(users, lessons).ExecuteAsync(
                    new ListReviewQueueCommand(languageId, reviewer, Skip: 0, Take: 100));

                var actualIds = queue.Lessons.Select(item => item.LessonId).ToHashSet();
                Assert.Equal(expectedIds, actualIds);
                Assert.All(queue.Lessons, item => Assert.Equal(languageId, item.LanguageId));

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

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
}
