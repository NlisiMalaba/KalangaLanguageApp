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
public sealed class StreakAndLevelUseCaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Consecutive_calendar_days_increment_streak_and_a_gap_resets_it()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, streaks) = await SeedAsync(db);
        var day = new DateOnly(2026, 8, 1);

        var first = await streaks.ExecuteAsync(
            new UpdateStreakCommand(languageId, learner, day, RecordActivity: true));
        Assert.Equal(1, first.CurrentStreak);
        Assert.False(first.Reset);

        var second = await streaks.ExecuteAsync(
            new UpdateStreakCommand(languageId, learner, day.AddDays(1), RecordActivity: true));
        Assert.Equal(2, second.CurrentStreak);
        Assert.Equal(2, second.LongestStreak);

        var sameDay = await streaks.ExecuteAsync(
            new UpdateStreakCommand(languageId, learner, day.AddDays(1), RecordActivity: true));
        Assert.Equal(2, sameDay.CurrentStreak);

        var stillOpen = await streaks.ExecuteAsync(
            new UpdateStreakCommand(languageId, learner, day.AddDays(2), RecordActivity: false));
        Assert.Equal(2, stillOpen.CurrentStreak);
        Assert.False(stillOpen.Reset);

        var missed = await streaks.ExecuteAsync(
            new UpdateStreakCommand(languageId, learner, day.AddDays(3), RecordActivity: false));
        Assert.Equal(0, missed.CurrentStreak);
        Assert.True(missed.Reset);
        Assert.Equal(day.AddDays(1), missed.LastActivityDate);

        var returned = await streaks.ExecuteAsync(
            new UpdateStreakCommand(languageId, learner, day.AddDays(3), RecordActivity: true));
        Assert.Equal(1, returned.CurrentStreak);
        Assert.Equal(2, returned.LongestStreak);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Advance_level_crosses_xp_thresholds()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var learner = await SeedUserAsync(users, languageId, now);
        var gamification = new GamificationRepository(db);
        var advance = new AdvanceLevelUseCase(users, gamification);

        var state = LearnerGamification.Create(languageId, learner, now);
        state.AwardXp(DomainRules.IntermediateXpThreshold, now);
        await gamification.AddAsync(languageId, state);
        Assert.Equal(Level.Beginner, (await gamification.FindByUserAsync(languageId, learner))!.ProgressLevel);

        var intermediate = await advance.ExecuteAsync(new AdvanceLevelCommand(languageId, learner));
        Assert.True(intermediate.Advanced);
        Assert.Equal(Level.Intermediate, intermediate.ProgressLevel);
        Assert.Equal(DomainRules.IntermediateXpThreshold, intermediate.TotalXp);

        var again = await advance.ExecuteAsync(new AdvanceLevelCommand(languageId, learner));
        Assert.False(again.Advanced);
        Assert.Equal(Level.Intermediate, again.ProgressLevel);

        var persisted = await gamification.FindByUserAsync(languageId, learner);
        persisted!.AwardXp(DomainRules.AdvancedXpThreshold - DomainRules.IntermediateXpThreshold, now);
        await gamification.UpdateAsync(languageId, persisted);

        var advanced = await advance.ExecuteAsync(new AdvanceLevelCommand(languageId, learner));
        Assert.True(advanced.Advanced);
        Assert.Equal(Level.Advanced, advanced.ProgressLevel);

        await transaction.RollbackAsync();
    }

    private static async Task<(LanguageId LanguageId, UserId Learner, UpdateStreakUseCase Streaks)> SeedAsync(
        KalangaDbContext db)
    {
        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var learner = await SeedUserAsync(users, languageId, now);
        var streaks = new UpdateStreakUseCase(users, new GamificationRepository(db));
        return (languageId, learner, streaks);
    }

    private static async Task<UserId> SeedUserAsync(UserRepository users, LanguageId languageId, DateTimeOffset now)
    {
        var user = User.Register(
            languageId,
            $"learner-{Guid.NewGuid():N}@example.com",
            "hash",
            "Learner",
            now);
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
