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
public sealed class XpThresholdTriggersLevelAdvancementPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 19: XP Threshold Triggers Level Advancement
    [Fact(Timeout = 180_000)]
    public async Task Progress_level_matches_xp_thresholds_and_advances_when_crossed()
    {
        var totalXp = Gen.Int[1, DomainRules.AdvancedXpThreshold + 500];

        await Check.SampleAsync(
            totalXp,
            async (int xp) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

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

                var gamification = new GamificationRepository(db);
                var state = LearnerGamification.Create(languageId, user.Id, now);
                state.AwardXp(xp, now);
                await gamification.AddAsync(languageId, state);

                var stored = await gamification.FindByUserAsync(languageId, user.Id);
                Assert.NotNull(stored);
                Assert.Equal(Level.Beginner, stored.ProgressLevel);
                Assert.Equal(xp, stored.TotalXp);

                var expected = xp >= DomainRules.AdvancedXpThreshold
                    ? Level.Advanced
                    : xp >= DomainRules.IntermediateXpThreshold
                        ? Level.Intermediate
                        : Level.Beginner;

                var advance = new AdvanceLevelUseCase(users, gamification);
                var first = await advance.ExecuteAsync(new AdvanceLevelCommand(languageId, user.Id));
                Assert.Equal(expected, first.ProgressLevel);
                Assert.Equal(xp, first.TotalXp);
                Assert.Equal(expected != Level.Beginner, first.Advanced);

                var second = await advance.ExecuteAsync(new AdvanceLevelCommand(languageId, user.Id));
                Assert.Equal(expected, second.ProgressLevel);
                Assert.False(second.Advanced);

                var persisted = await gamification.FindByUserAsync(languageId, user.Id);
                Assert.NotNull(persisted);
                Assert.Equal(expected, persisted.ProgressLevel);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }
}
