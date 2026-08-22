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
public sealed class LessonCatalogFilterInvariantPropertyTests(PostgresFixture postgres)
{
    private static readonly string[] Categories = ["Everyday", "Travel", "Family", "Work"];

    // Feature: kalanga-language-app, Property 5: Lesson Catalog Filter Invariant
    [Fact(Timeout = 180_000)]
    public async Task Catalog_contains_exactly_published_lessons_matching_language_level_and_category()
    {
        var lessonSpec =
            from otherTenant in Gen.Bool
            from status in Gen.Enum<LessonStatus>()
            from level in Gen.Enum<Level>()
            from category in Gen.OneOfConst(Categories)
            select (otherTenant, status, level, category);

        var catalogQuery =
            from specs in lessonSpec.Array[0, 8]
            from applyLevel in Gen.Bool
            from filterLevel in Gen.Enum<Level>()
            from applyCategory in Gen.Bool
            from filterCategory in Gen.OneOfConst(Categories)
            select (
                Specs: specs,
                FilterLevel: applyLevel ? filterLevel : (Level?)null,
                FilterCategory: applyCategory ? filterCategory : null);

        await Check.SampleAsync(
            catalogQuery,
            async ((bool OtherTenant, LessonStatus Status, Level Level, string Category)[] specs, Level? filterLevel, string? filterCategory) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                var otherLanguageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);
                await SeedLanguageAsync(db, otherLanguageId);

                var now = DateTimeOffset.UtcNow;
                var users = new UserRepository(db);
                var contributor = await SeedContributorAsync(users, languageId, now, "a");
                var otherContributor = await SeedContributorAsync(users, otherLanguageId, now, "b");
                var reviewer = await SeedReviewerAsync(users, languageId, now, "a");
                var otherReviewer = await SeedReviewerAsync(users, otherLanguageId, now, "b");

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
                        $"L{i:D2}-{spec.Status}-{spec.Level}-{spec.Category}",
                        spec.Level,
                        spec.Category,
                        spec.Status,
                        now);
                    await lessons.AddAsync(tenant, lesson);
                    seeded.Add(lesson);
                }

                var expectedIds = seeded
                    .Where(lesson => lesson.LanguageId == languageId && lesson.Status == LessonStatus.Published)
                    .Where(lesson => filterLevel is null || lesson.Level == filterLevel)
                    .Where(lesson => filterCategory is null || lesson.Category == filterCategory)
                    .OrderBy(lesson => lesson.Title, StringComparer.Ordinal)
                    .Select(lesson => lesson.Id)
                    .ToArray();

                var browse = new BrowseLessonCatalogUseCase(lessons);
                var result = await browse.ExecuteAsync(
                    new BrowseLessonCatalogCommand(languageId, filterLevel, filterCategory, Skip: 0, Take: 100));

                Assert.All(result.Lessons, item =>
                {
                    Assert.Equal(languageId, item.LanguageId);
                    if (filterLevel is { } level)
                    {
                        Assert.Equal(level, item.Level);
                    }

                    if (filterCategory is not null)
                    {
                        Assert.Equal(filterCategory, item.Category);
                    }
                });

                var actualIds = result.Lessons.Select(item => item.LessonId).ToArray();
                Assert.Equal(expectedIds, actualIds);

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

    private static async Task<UserId> SeedContributorAsync(
        UserRepository users,
        LanguageId languageId,
        DateTimeOffset now,
        string suffix)
    {
        var contributor = User.Register(
            languageId,
            $"contrib-{suffix}-{Guid.NewGuid():N}@example.com",
            "hash",
            "Contributor",
            now);
        contributor.ChangeRole(Role.Contributor, now);
        await users.AddAsync(languageId, contributor);
        return contributor.Id;
    }

    private static async Task<UserId> SeedReviewerAsync(
        UserRepository users,
        LanguageId languageId,
        DateTimeOffset now,
        string suffix)
    {
        var reviewer = User.Register(
            languageId,
            $"review-{suffix}-{Guid.NewGuid():N}@example.com",
            "hash",
            "Reviewer",
            now);
        reviewer.ChangeRole(Role.Reviewer, now);
        await users.AddAsync(languageId, reviewer);
        return reviewer.Id;
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
