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
public sealed class StreakIncrementsOnConsecutiveDailyActivityPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 17: Streak Increments on Consecutive Daily Activity
    [Fact(Timeout = 180_000)]
    public async Task Consecutive_calendar_day_activity_increments_streak_by_one_each_day()
    {
        var input =
            from startOffset in Gen.Int[0, 40]
            from days in Gen.Int[2, 10]
            select (startOffset, days);

        await Check.SampleAsync(
            input,
            async (int startOffset, int days) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var (languageId, learner, streaks) = await SeedAsync(db);
                var start = new DateOnly(2026, 1, 1).AddDays(startOffset);

                UpdateStreakResult? last = null;
                for (var i = 0; i < days; i++)
                {
                    last = await streaks.ExecuteAsync(
                        new UpdateStreakCommand(languageId, learner, start.AddDays(i), RecordActivity: true));
                    Assert.Equal(i + 1, last.CurrentStreak);
                    Assert.Equal(i + 1, last.LongestStreak);
                    Assert.Equal(start.AddDays(i), last.LastActivityDate);
                    Assert.False(last.Reset);
                }

                var sameDay = await streaks.ExecuteAsync(
                    new UpdateStreakCommand(languageId, learner, start.AddDays(days - 1), RecordActivity: true));
                Assert.Equal(days, sameDay.CurrentStreak);
                Assert.Equal(days, sameDay.LongestStreak);
                Assert.Equal(last!.CurrentStreak, sameDay.CurrentStreak);

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
