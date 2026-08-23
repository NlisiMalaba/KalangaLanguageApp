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
public sealed class LessonCompletionRecordsProgressAndAwardsXpPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 8: Lesson Completion Records Progress and Awards XP
    [Fact(Timeout = 180_000)]
    public async Task Completing_a_published_lesson_persists_progress_and_awards_xp_once()
    {
        var input =
            from score in Gen.Int[0, 100]
            from retryScore in Gen.Int[0, 100]
            from xpReward in Gen.Int[1, 50]
            select new Sample(score, retryScore, xpReward);

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
                var progress = new LearnerProgressRepository(db);
                var gamification = new GamificationRepository(db);
                var complete = new CompleteLessonUseCase(users, lessons, progress, gamification);

                var lesson = Lesson.CreateDraft(
                    languageId,
                    contributor,
                    "greetings",
                    Level.Beginner,
                    "Everyday",
                    now,
                    xpReward: sample.XpReward);
                lesson.SubmitForReview(now);
                lesson.Approve(reviewer, now);
                await lessons.AddAsync(languageId, lesson);

                var first = await complete.ExecuteAsync(
                    new CompleteLessonCommand(languageId, learner, lesson.Id, sample.Score));

                Assert.True(first.XpGranted);
                Assert.Equal(sample.Score, first.Score);
                Assert.Equal(sample.XpReward, first.XpAwarded);
                Assert.Equal(sample.XpReward, first.TotalXp);

                var persisted = await progress.FindByUserAndLessonAsync(languageId, learner, lesson.Id);
                Assert.NotNull(persisted);
                Assert.True(persisted.IsCompleted);
                Assert.NotNull(persisted.CompletedAt);
                Assert.Equal(sample.Score, persisted.Score);
                Assert.Equal(sample.XpReward, persisted.XpAwarded);

                var totals = await gamification.FindByUserAsync(languageId, learner);
                Assert.NotNull(totals);
                Assert.Equal(sample.XpReward, totals.TotalXp);

                var second = await complete.ExecuteAsync(
                    new CompleteLessonCommand(languageId, learner, lesson.Id, sample.RetryScore));

                Assert.False(second.XpGranted);
                Assert.Equal(sample.RetryScore, second.Score);
                Assert.Equal(sample.XpReward, second.XpAwarded);
                Assert.Equal(sample.XpReward, second.TotalXp);

                var afterRetry = await gamification.FindByUserAsync(languageId, learner);
                Assert.NotNull(afterRetry);
                Assert.Equal(sample.XpReward, afterRetry.TotalXp);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private sealed record Sample(int Score, int RetryScore, int XpReward);

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
