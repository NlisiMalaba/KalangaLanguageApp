using CsCheck;
using Kalanga.Application.Dtos;
using Kalanga.Application.UseCases;
using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class StreakResetsOnMissedDayPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 18: Streak Resets on Missed Day
    [Fact(Timeout = 180_000)]
    public async Task Streak_resets_to_zero_only_after_a_missed_calendar_day()
    {
        var input =
            from streakDays in Gen.Int[1, 6]
            from gapDays in Gen.Int[2, 6]
            select (streakDays, gapDays);

        await Check.SampleAsync(
            input,
            async (int streakDays, int gapDays) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var (languageId, learner, streaks) = await SeedAsync(db);
                var start = new DateOnly(2026, 3, 1);

                for (var i = 0; i < streakDays; i++)
                {
                    await streaks.ExecuteAsync(
                        new UpdateStreakCommand(languageId, learner, start.AddDays(i), RecordActivity: true));
                }

                var lastActivity = start.AddDays(streakDays - 1);

                var stillOpen = await streaks.ExecuteAsync(
                    new UpdateStreakCommand(languageId, learner, lastActivity.AddDays(1), RecordActivity: false));
                Assert.Equal(streakDays, stillOpen.CurrentStreak);
                Assert.False(stillOpen.Reset);

                var missed = await streaks.ExecuteAsync(
                    new UpdateStreakCommand(languageId, learner, lastActivity.AddDays(gapDays), RecordActivity: false));
                Assert.Equal(0, missed.CurrentStreak);
                Assert.True(missed.Reset);
                Assert.Equal(lastActivity, missed.LastActivityDate);
                Assert.Equal(streakDays, missed.LongestStreak);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static async Task<(LanguageId LanguageId, UserId Learner, UpdateStreakUseCase Streaks)> SeedAsync(
        KalangaDbContext db)
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
        var user = User.Register(languageId, $"learner-{Guid.NewGuid():N}@example.com", "hash", "Learner", now);
        await users.AddAsync(languageId, user);
        return (languageId, user.Id, new UpdateStreakUseCase(users, new GamificationRepository(db)));
    }
}
