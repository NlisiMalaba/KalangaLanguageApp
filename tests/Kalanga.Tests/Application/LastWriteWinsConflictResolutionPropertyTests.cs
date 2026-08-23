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
public sealed class LastWriteWinsConflictResolutionPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 26: Last-Write-Wins Conflict Resolution
    [Fact(Timeout = 180_000)]
    public async Task Conflicting_sync_writes_keep_the_newer_progress_and_merge_gamification_additively()
    {
        var epoch = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var input =
            from firstOffset in Gen.Int[0, 10_000]
            from secondOffset in Gen.Int[0, 10_000]
            from firstScore in Gen.Int[0, 100]
            from secondScore in Gen.Int[0, 100]
            from firstInterval in Gen.Int[1, 20]
            from secondInterval in Gen.Int[1, 20]
            from firstStreak in Gen.Int[0, 30]
            from secondStreak in Gen.Int[0, 30]
            from firstLongest in Gen.Int[0, 40]
            from secondLongest in Gen.Int[0, 40]
            from firstDelta in Gen.Int[0, 25]
            from secondDelta in Gen.Int[0, 25]
            select new ConflictSample(
                epoch.AddSeconds(firstOffset),
                epoch.AddSeconds(secondOffset),
                firstScore,
                secondScore,
                firstInterval,
                secondInterval,
                firstStreak,
                secondStreak,
                Math.Max(firstStreak, firstLongest),
                Math.Max(secondStreak, secondLongest),
                firstDelta,
                secondDelta);

        await Check.SampleAsync(
            input,
            async (ConflictSample sample) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var (languageId, learner, lesson, phrase, sync) = await SeedAsync(db);

                await sync.ExecuteAsync(BuildCommand(languageId, learner, lesson.Id, phrase.Id, sample.First));
                var result = await sync.ExecuteAsync(BuildCommand(languageId, learner, lesson.Id, phrase.Id, sample.Second));

                var winner = sample.Second.UpdatedAt > sample.First.UpdatedAt ? sample.Second : sample.First;
                var progress = Assert.Single(result.ServerChanges.Progress);
                Assert.Equal(winner.Score, progress.Score);
                Assert.Equal(winner.UpdatedAt, progress.UpdatedAt);

                var srs = Assert.Single(result.ServerChanges.SpacedRepetition);
                Assert.Equal(winner.IntervalDays, srs.IntervalDays);
                Assert.Equal(winner.UpdatedAt, srs.UpdatedAt);

                var totals = result.ServerChanges.Gamification;
                Assert.NotNull(totals);
                Assert.Equal(10 + sample.First.XpDelta + sample.Second.XpDelta, totals.TotalXp);
                Assert.Equal(Math.Max(sample.First.CurrentStreak, sample.Second.CurrentStreak), totals.CurrentStreak);
                Assert.Equal(
                    Math.Max(
                        Math.Max(sample.First.LongestStreak, sample.Second.LongestStreak),
                        totals.CurrentStreak),
                    totals.LongestStreak);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static SyncProgressCommand BuildCommand(
        LanguageId languageId,
        UserId learner,
        LessonId lessonId,
        PhraseId phraseId,
        WriteSample write)
    {
        return new SyncProgressCommand(
            languageId,
            learner,
            Guid.NewGuid().ToString("N"),
            [new SyncProgressItemDto(lessonId, write.UpdatedAt, write.Score, XpAwarded: 10, write.UpdatedAt)],
            [
                new SyncSrsItemDto(
                    phraseId,
                    VariationId: null,
                    EaseFactor: 2.5m,
                    write.IntervalDays,
                    Repetitions: 1,
                    DateOnly.FromDateTime(write.UpdatedAt.UtcDateTime).AddDays(write.IntervalDays),
                    write.UpdatedAt,
                    write.UpdatedAt),
            ],
            new SyncGamificationDto(
                write.XpDelta,
                write.CurrentStreak,
                write.LongestStreak,
                DateOnly.FromDateTime(write.UpdatedAt.UtcDateTime),
                Level.Beginner,
                write.XpDelta,
                write.UpdatedAt));
    }

    private static async Task<(
        LanguageId LanguageId,
        UserId Learner,
        Lesson Lesson,
        Phrase Phrase,
        SyncProgressUseCase Sync)> SeedAsync(KalangaDbContext db)
    {
        var languageId = LanguageId.New();
        db.Languages.Add(new LanguageRecord
        {
            Id = languageId.Value,
            Code = languageId.Value.ToString("N")[..10],
            Name = "Test Language",
            Region = "Test",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var learner = User.Register(languageId, $"learner-{Guid.NewGuid():N}@example.com", "hash", "Learner", now);
        var contributor = User.Register(languageId, $"contrib-{Guid.NewGuid():N}@example.com", "hash", "Contributor", now);
        contributor.ChangeRole(Role.Contributor, now);
        var reviewer = User.Register(languageId, $"review-{Guid.NewGuid():N}@example.com", "hash", "Reviewer", now);
        reviewer.ChangeRole(Role.Reviewer, now);
        await users.AddAsync(languageId, learner);
        await users.AddAsync(languageId, contributor);
        await users.AddAsync(languageId, reviewer);

        var lessons = new LessonRepository(db);
        var lesson = Lesson.CreateDraft(languageId, contributor.Id, "Greetings", Level.Beginner, "Everyday", now);
        lesson.SubmitForReview(now);
        lesson.Approve(reviewer.Id, now);
        await lessons.AddAsync(languageId, lesson);

        var phrases = new PhraseRepository(db);
        var phrase = Phrase.Create(languageId, lesson.Id, "Ndini", "I am", sortOrder: 0, now);
        await phrases.AddAsync(languageId, phrase);

        var sync = new SyncProgressUseCase(
            users,
            lessons,
            phrases,
            new LanguageVariationRepository(db),
            new LearnerProgressRepository(db),
            new SpacedRepetitionRepository(db),
            new GamificationRepository(db),
            new SyncCheckpointRepository(db),
            new SyncPushReceiptRepository(db),
            new EfUnitOfWork(db));

        return (languageId, learner.Id, lesson, phrase, sync);
    }

    private sealed record WriteSample(
        DateTimeOffset UpdatedAt,
        int Score,
        int IntervalDays,
        int CurrentStreak,
        int LongestStreak,
        int XpDelta);

    private sealed record ConflictSample(
        DateTimeOffset FirstUpdatedAt,
        DateTimeOffset SecondUpdatedAt,
        int FirstScore,
        int SecondScore,
        int FirstInterval,
        int SecondInterval,
        int FirstStreak,
        int SecondStreak,
        int FirstLongest,
        int SecondLongest,
        int FirstDelta,
        int SecondDelta)
    {
        public WriteSample First => new(FirstUpdatedAt, FirstScore, FirstInterval, FirstStreak, FirstLongest, FirstDelta);

        public WriteSample Second => new(SecondUpdatedAt, SecondScore, SecondInterval, SecondStreak, SecondLongest, SecondDelta);
    }
}
