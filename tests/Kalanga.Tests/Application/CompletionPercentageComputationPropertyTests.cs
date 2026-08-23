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
public sealed class CompletionPercentageComputationPropertyTests(PostgresFixture postgres)
{
    private static readonly string[] Categories = ["Everyday", "Travel", "Family"];

    // Feature: kalanga-language-app, Property 21: Completion Percentage Computation
    [Fact(Timeout = 180_000)]
    public async Task Completion_percentage_is_completed_published_lessons_over_published_in_the_bucket()
    {
        var input =
            from level in Gen.Enum<Level>()
            from category in Gen.OneOfConst(Categories)
            from completeFlags in Gen.Bool.Array[1, 6]
            select new Sample(level, category, completeFlags);

        await Check.SampleAsync(
            input,
            async (Sample sample) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);
                var now = DateTimeOffset.UtcNow;
                var users = new UserRepository(db);
                var learner = await SeedUserAsync(users, languageId, Role.Learner, now, "l");
                var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now, "c");
                var reviewer = await SeedUserAsync(users, languageId, Role.Reviewer, now, "r");
                var lessons = new LessonRepository(db);
                var progressRepo = new LearnerProgressRepository(db);
                var gamification = new GamificationRepository(db);
                var complete = new CompleteLessonUseCase(users, lessons, progressRepo, gamification);
                var getProgress = new GetProgressUseCase(users, lessons, progressRepo, gamification);

                var draft = Lesson.CreateDraft(languageId, contributor, "draft-hidden", sample.Level, sample.Category, now);
                await lessons.AddAsync(languageId, draft);

                var publishedIds = new List<LessonId>();
                for (var i = 0; i < sample.CompleteFlags.Length; i++)
                {
                    var lesson = Lesson.CreateDraft(
                        languageId,
                        contributor,
                        $"lesson-{i}",
                        sample.Level,
                        sample.Category,
                        now);
                    lesson.SubmitForReview(now);
                    lesson.Approve(reviewer, now);
                    await lessons.AddAsync(languageId, lesson);
                    publishedIds.Add(lesson.Id);
                }

                var completed = 0;
                for (var i = 0; i < sample.CompleteFlags.Length; i++)
                {
                    if (!sample.CompleteFlags[i])
                    {
                        continue;
                    }

                    await complete.ExecuteAsync(
                        new CompleteLessonCommand(languageId, learner, publishedIds[i], Score: 80));
                    completed++;
                }

                var result = await getProgress.ExecuteAsync(new GetProgressCommand(languageId, learner));
                var total = sample.CompleteFlags.Length;
                var expected = Math.Round(100m * completed / total, 1, MidpointRounding.AwayFromZero);

                var byLevel = Assert.Single(result.ByLevel, item => item.Level == sample.Level);
                Assert.Equal(completed, byLevel.CompletedCount);
                Assert.Equal(total, byLevel.TotalCount);
                Assert.Equal(expected, byLevel.Percentage);

                var byCategory = Assert.Single(result.ByCategory, item => item.Category == sample.Category);
                Assert.Equal(completed, byCategory.CompletedCount);
                Assert.Equal(total, byCategory.TotalCount);
                Assert.Equal(expected, byCategory.Percentage);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private sealed record Sample(Level Level, string Category, bool[] CompleteFlags);

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
            Code = languageId.Value.ToString("N")[^10..],
            Name = "Test Language",
            Region = "Test",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
