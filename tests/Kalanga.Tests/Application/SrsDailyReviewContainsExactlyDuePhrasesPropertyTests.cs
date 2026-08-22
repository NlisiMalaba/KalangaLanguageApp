using CsCheck;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class SrsDailyReviewContainsExactlyDuePhrasesPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 16: SRS Daily Review Contains Exactly Due Phrases
    [Fact(Timeout = 180_000)]
    public async Task FindDue_returns_exactly_the_records_with_next_review_on_or_before_as_of_date()
    {
        var offsets = Gen.Int[-6, 8].Array[1, 6];

        await Check.SampleAsync(
            offsets,
            async (int[] dayOffsets) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
                var today = DateOnly.FromDateTime(now.UtcDateTime);
                var (languageId, learner, other, phrases, srs) = await SeedAsync(db, now, dayOffsets.Length);

                var expectedDue = new HashSet<PhraseId>();
                for (var i = 0; i < dayOffsets.Length; i++)
                {
                    var record = SpacedRepetitionRecord.Create(languageId, learner, phrases[i].Id, now);
                    record.Schedule(
                        record.EaseFactor,
                        intervalDays: 1,
                        repetitions: 0,
                        today.AddDays(dayOffsets[i]),
                        now);
                    await srs.AddAsync(languageId, record);
                    if (dayOffsets[i] <= 0)
                    {
                        expectedDue.Add(phrases[i].Id);
                    }
                }

                var otherDue = SpacedRepetitionRecord.Create(languageId, other, phrases[0].Id, now);
                otherDue.Schedule(otherDue.EaseFactor, 1, 0, today.AddDays(-1), now);
                await srs.AddAsync(languageId, otherDue);

                var due = await srs.FindDueAsync(languageId, learner, today);
                var duePhrases = due.Select(item => item.PhraseId).ToHashSet();

                Assert.Equal(expectedDue, duePhrases);
                Assert.All(due, item =>
                {
                    Assert.Equal(learner, item.UserId);
                    Assert.True(item.NextReviewAt <= today);
                });

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static async Task<(
        LanguageId LanguageId,
        UserId Learner,
        UserId Other,
        IReadOnlyList<Phrase> Phrases,
        SpacedRepetitionRepository Srs)> SeedAsync(KalangaDbContext db, DateTimeOffset now, int phraseCount)
    {
        var languageId = LanguageId.New();
        db.Languages.Add(new LanguageRecord
        {
            Id = languageId.Value,
            Code = languageId.Value.ToString("N")[..10],
            Name = "Test Language",
            Region = "Test",
            IsActive = true,
            CreatedAt = now,
        });
        await db.SaveChangesAsync();

        var users = new UserRepository(db);
        var learner = User.Register(languageId, $"learner-{Guid.NewGuid():N}@example.com", "hash", "Learner", now);
        var other = User.Register(languageId, $"other-{Guid.NewGuid():N}@example.com", "hash", "Other", now);
        var contributor = User.Register(languageId, $"contrib-{Guid.NewGuid():N}@example.com", "hash", "Contributor", now);
        contributor.ChangeRole(Role.Contributor, now);
        await users.AddAsync(languageId, learner);
        await users.AddAsync(languageId, other);
        await users.AddAsync(languageId, contributor);

        var lessons = new LessonRepository(db);
        var lesson = Lesson.CreateDraft(languageId, contributor.Id, "Greetings", Level.Beginner, "Everyday", now);
        await lessons.AddAsync(languageId, lesson);

        var phraseRepo = new PhraseRepository(db);
        var phrases = new List<Phrase>();
        for (var i = 0; i < phraseCount; i++)
        {
            var phrase = Phrase.Create(languageId, lesson.Id, $"ndini-{i}", $"I am {i}", i, now);
            await phraseRepo.AddAsync(languageId, phrase);
            phrases.Add(phrase);
        }

        return (languageId, learner.Id, other.Id, phrases, new SpacedRepetitionRepository(db));
    }
}
