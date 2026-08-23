using CsCheck;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Services;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class SrsTracksLanguageVariationsIndependentlyPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 38: SRS Tracks Language Variations Independently
    [Fact(Timeout = 180_000)]
    public async Task Base_phrase_and_variation_cards_do_not_share_srs_state()
    {
        var input =
            from baseCorrect in Gen.Bool
            from variationCorrect in Gen.Bool
            select (baseCorrect, variationCorrect);

        var srs = new SpacedRepetitionService();

        await Check.SampleAsync(
            input,
            async (bool baseCorrect, bool variationCorrect) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
                var (languageId, learner, phrase, variation, repo) = await SeedAsync(db, now);

                var baseCard = SpacedRepetitionRecord.Create(languageId, learner, phrase.Id, now);
                var variationCard = SpacedRepetitionRecord.Create(
                    languageId,
                    learner,
                    phrase.Id,
                    now,
                    variation.Id);

                srs.RecordAnswer(baseCard, baseCorrect, now);
                srs.RecordAnswer(variationCard, variationCorrect, now);
                await repo.AddAsync(languageId, baseCard);
                await repo.AddAsync(languageId, variationCard);

                var loadedBase = await repo.FindByUserPhraseAndVariationAsync(
                    languageId, learner, phrase.Id, variationId: null);
                var loadedVariation = await repo.FindByUserPhraseAndVariationAsync(
                    languageId, learner, phrase.Id, variation.Id);

                Assert.NotNull(loadedBase);
                Assert.NotNull(loadedVariation);
                Assert.NotEqual(loadedBase.Id, loadedVariation.Id);
                Assert.Null(loadedBase.VariationId);
                Assert.Equal(variation.Id, loadedVariation.VariationId);
                Assert.Equal(baseCard.IntervalDays, loadedBase.IntervalDays);
                Assert.Equal(variationCard.IntervalDays, loadedVariation.IntervalDays);
                Assert.Equal(baseCard.Repetitions, loadedBase.Repetitions);
                Assert.Equal(variationCard.Repetitions, loadedVariation.Repetitions);

                var intervalBefore = loadedVariation.IntervalDays;
                srs.RecordAnswer(loadedBase, isCorrect: true, now);
                await repo.UpdateAsync(languageId, loadedBase);

                var variationAfter = await repo.FindByUserPhraseAndVariationAsync(
                    languageId, learner, phrase.Id, variation.Id);
                Assert.NotNull(variationAfter);
                Assert.Equal(intervalBefore, variationAfter.IntervalDays);
                Assert.Equal(variationCard.EaseFactor, variationAfter.EaseFactor);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static async Task<(
        LanguageId LanguageId,
        UserId Learner,
        Phrase Phrase,
        LanguageVariation Variation,
        SpacedRepetitionRepository Srs)> SeedAsync(KalangaDbContext db, DateTimeOffset now)
    {
        var languageId = LanguageId.New();
        db.Languages.Add(new LanguageRecord
        {
            Id = languageId.Value,
            Code = languageId.Value.ToString("N")[^10..],
            Name = "Test Language",
            Region = "Test",
            IsActive = true,
            CreatedAt = now,
        });
        await db.SaveChangesAsync();

        var users = new UserRepository(db);
        var learner = User.Register(languageId, $"learner-{Guid.NewGuid():N}@example.com", "hash", "Learner", now);
        var contributor = User.Register(languageId, $"contrib-{Guid.NewGuid():N}@example.com", "hash", "Contributor", now);
        contributor.ChangeRole(Role.Contributor, now);
        await users.AddAsync(languageId, learner);
        await users.AddAsync(languageId, contributor);

        var lesson = Lesson.CreateDraft(languageId, contributor.Id, "Greetings", Level.Beginner, "Everyday", now);
        await new LessonRepository(db).AddAsync(languageId, lesson);

        var phrase = Phrase.Create(languageId, lesson.Id, "Ndini", "I am", 0, now);
        await new PhraseRepository(db).AddAsync(languageId, phrase);

        var variation = LanguageVariation.Create(languageId, phrase.Id, "Ndini zwino", "everyday", now);
        await new LanguageVariationRepository(db).AddAsync(languageId, variation);

        return (languageId, learner.Id, phrase, variation, new SpacedRepetitionRepository(db));
    }
}
