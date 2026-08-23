using CsCheck;
using Kalanga.Application.Dtos;
using Kalanga.Application.UseCases;
using Kalanga.Domain;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class WeakAreaIdentificationBelowThresholdPropertyTests(PostgresFixture postgres)
{
    private static readonly string[] Categories = ["Everyday", "Travel", "Family"];

    // Feature: kalanga-language-app, Property 20: Weak Area Identification Below Threshold
    [Fact(Timeout = 180_000)]
    public async Task Weak_areas_are_exactly_the_buckets_whose_average_score_is_below_threshold()
    {
        var input =
            from level in Gen.Enum<Level>()
            from category in Gen.OneOfConst(Categories)
            from scores in Gen.Int[0, 100].Array[1, 4]
            select new Sample(level, category, scores);

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

                var strong = Lesson.CreateDraft(
                    languageId,
                    contributor,
                    "strong-control",
                    sample.Level,
                    "Work",
                    now);
                strong.SubmitForReview(now);
                strong.Approve(reviewer, now);
                await lessons.AddAsync(languageId, strong);
                await complete.ExecuteAsync(
                    new CompleteLessonCommand(languageId, learner, strong.Id, DomainRules.WeakAreaScoreThreshold));

                for (var i = 0; i < sample.Scores.Length; i++)
                {
                    var lesson = Lesson.CreateDraft(
                        languageId,
                        contributor,
                        $"target-{i}",
                        sample.Level,
                        sample.Category,
                        now);
                    lesson.SubmitForReview(now);
                    lesson.Approve(reviewer, now);
                    await lessons.AddAsync(languageId, lesson);
                    await complete.ExecuteAsync(
                        new CompleteLessonCommand(languageId, learner, lesson.Id, sample.Scores[i]));
                }

                var expectedAverage = Math.Round(
                    (decimal)sample.Scores.Average(),
                    1,
                    MidpointRounding.AwayFromZero);
                var result = await getProgress.ExecuteAsync(new GetProgressCommand(languageId, learner));

                Assert.DoesNotContain(
                    result.WeakAreas,
                    area => area.Level == sample.Level && area.Category == "Work");

                var match = result.WeakAreas.SingleOrDefault(
                    area => area.Level == sample.Level && area.Category == sample.Category);

                if (expectedAverage < DomainRules.WeakAreaScoreThreshold)
                {
                    Assert.NotNull(match);
                    Assert.Equal(expectedAverage, match.AverageScore);
                    Assert.Equal(sample.Scores.Length, match.SampleSize);
                }
                else
                {
                    Assert.Null(match);
                }

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private sealed record Sample(Level Level, string Category, int[] Scores);

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
