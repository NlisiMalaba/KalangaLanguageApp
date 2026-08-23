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
public sealed class LessonDraftSaveRoundTripPropertyTests(PostgresFixture postgres)
{
    private static readonly string[] Categories = ["Everyday", "Travel", "Family", "Work"];

    // Feature: kalanga-language-app, Property 33: Lesson Draft Save Round-Trip
    [Fact(Timeout = 180_000)]
    public async Task Saving_a_draft_persists_fields_exactly_on_reload()
    {
        var text = Gen.String[Gen.Char['a', 'z'], 3, 40];

        var draftSave =
            from initialTitle in text
            from initialLevel in Gen.Enum<Level>()
            from initialCategory in Gen.OneOfConst(Categories)
            from savedTitle in text
            from savedLevel in Gen.Enum<Level>()
            from savedCategory in Gen.OneOfConst(Categories)
            from isScenario in Gen.Bool
            from scenarioContext in text
            from xpReward in Gen.Int[1, 100]
            select new DraftSaveSample(
                initialTitle,
                initialLevel,
                initialCategory,
                savedTitle,
                savedLevel,
                savedCategory,
                isScenario,
                isScenario ? scenarioContext : null,
                xpReward);

        await Check.SampleAsync(
            draftSave,
            async (DraftSaveSample sample) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);

                var now = DateTimeOffset.UtcNow;
                var users = new UserRepository(db);
                var contributor = await SeedContributorAsync(users, languageId, now);
                var lessons = new LessonRepository(db);

                var created = await new CreateLessonUseCase(users, lessons).ExecuteAsync(
                    new CreateLessonCommand(
                        languageId,
                        contributor,
                        sample.InitialTitle,
                        sample.InitialLevel,
                        sample.InitialCategory));

                var saved = await new SaveLessonDraftUseCase(users, lessons).ExecuteAsync(
                    new SaveLessonDraftCommand(
                        languageId,
                        contributor,
                        created.LessonId,
                        sample.SavedTitle,
                        sample.SavedLevel,
                        sample.SavedCategory,
                        sample.IsScenario,
                        sample.ScenarioContext,
                        sample.XpReward));

                Assert.Equal(LessonStatus.Draft, saved.Status);
                Assert.Equal(sample.SavedTitle, saved.Title);
                Assert.Equal(sample.SavedLevel, saved.Level);
                Assert.Equal(sample.SavedCategory, saved.Category);
                Assert.Equal(sample.IsScenario, saved.IsScenario);
                Assert.Equal(sample.ScenarioContext, saved.ScenarioContext);
                Assert.Equal(sample.XpReward, saved.XpReward);

                var reloaded = await lessons.FindByIdAsync(languageId, created.LessonId);
                Assert.NotNull(reloaded);
                Assert.Equal(LessonStatus.Draft, reloaded.Status);
                Assert.Equal(sample.SavedTitle, reloaded.Title);
                Assert.Equal(sample.SavedLevel, reloaded.Level);
                Assert.Equal(sample.SavedCategory, reloaded.Category);
                Assert.Equal(sample.IsScenario, reloaded.IsScenario);
                Assert.Equal(sample.ScenarioContext, reloaded.ScenarioContext);
                Assert.Equal(sample.XpReward, reloaded.XpReward);
                Assert.Equal(contributor, reloaded.ContributorId);
                Assert.False(reloaded.IsVisibleInCatalog);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static async Task<UserId> SeedContributorAsync(UserRepository users, LanguageId languageId, DateTimeOffset now)
    {
        var contributor = User.Register(
            languageId,
            $"contrib-{Guid.NewGuid():N}@example.com",
            "hash",
            "Contributor",
            now);
        contributor.ChangeRole(Role.Contributor, now);
        await users.AddAsync(languageId, contributor);
        return contributor.Id;
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

    private sealed record DraftSaveSample(
        string InitialTitle,
        Level InitialLevel,
        string InitialCategory,
        string SavedTitle,
        Level SavedLevel,
        string SavedCategory,
        bool IsScenario,
        string? ScenarioContext,
        int XpReward);
}
