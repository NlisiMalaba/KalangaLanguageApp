using CsCheck;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Tests.Infrastructure;

[Collection(PostgresCollection.Name)]
public sealed class TenantIsolationPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property: tenant isolation
    [Fact(Timeout = 180_000)]
    public async Task Repository_results_never_include_rows_from_another_language()
    {
        var distinctLanguageIds =
            from queried in Gen.Guid.Where(id => id != Guid.Empty)
            from other in Gen.Guid.Where(id => id != Guid.Empty && id != queried)
            select (queried, other);

        await Check.SampleAsync(
            distinctLanguageIds,
            async (Guid queriedGuid, Guid otherGuid) =>
            {
                var queried = LanguageId.From(queriedGuid);
                var other = LanguageId.From(otherGuid);

                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var queriedGraph = await SeedTenantAsync(db, queried, prefix: "q");
                var otherGraph = await SeedTenantAsync(db, other, prefix: "o");

                var users = new UserRepository(db);
                var lessons = new LessonRepository(db);
                var phrases = new PhraseRepository(db);
                var audio = new AudioRecordingRepository(db);
                var exercises = new ExerciseRepository(db);
                var packs = new ContentPackRepository(db);
                var progress = new LearnerProgressRepository(db);
                var srs = new SpacedRepetitionRepository(db);
                var gamification = new GamificationRepository(db);
                var requests = new RequestRepository(db);
                var checkpoints = new SyncCheckpointRepository(db);

                Assert.Null(await users.FindByIdAsync(queried, otherGraph.LearnerId));
                Assert.Null(await users.FindByEmailAsync(queried, otherGraph.LearnerEmail));
                AssertOnlyTenant(await users.ListAsync(queried, skip: 0, take: 50), queried, user => user.LanguageId);

                Assert.Null(await lessons.FindByIdAsync(queried, otherGraph.PublishedLessonId));
                Assert.Empty(await lessons.FindByIdsAsync(queried, [otherGraph.PublishedLessonId]));
                AssertOnlyTenant(
                    await lessons.FindByIdsAsync(queried, [queriedGraph.PublishedLessonId, otherGraph.PublishedLessonId]),
                    queried,
                    lesson => lesson.LanguageId);
                AssertOnlyTenant(
                    await lessons.FindPublishedAsync(queried, level: null, category: null, skip: 0, take: 50),
                    queried,
                    lesson => lesson.LanguageId);
                Assert.DoesNotContain(
                    await lessons.FindPublishedSummariesAsync(queried),
                    summary => summary.LessonId == otherGraph.PublishedLessonId);
                Assert.Contains(
                    await lessons.FindPublishedSummariesAsync(queried),
                    summary => summary.LessonId == queriedGraph.PublishedLessonId);
                AssertOnlyTenant(
                    await lessons.FindPendingReviewAsync(queried, skip: 0, take: 50),
                    queried,
                    lesson => lesson.LanguageId);
                Assert.Empty(await lessons.FindByContributorAsync(queried, otherGraph.ContributorId, status: null));
                AssertOnlyTenant(
                    await lessons.FindByContributorAsync(queried, queriedGraph.ContributorId, status: null),
                    queried,
                    lesson => lesson.LanguageId);

                Assert.Null(await phrases.FindByIdAsync(queried, otherGraph.PhraseId));
                Assert.Empty(await phrases.FindByLessonIdAsync(queried, otherGraph.PublishedLessonId));
                Assert.Empty(await phrases.FindByLessonIdsAsync(queried, [otherGraph.PublishedLessonId]));
                AssertOnlyTenant(
                    await phrases.FindByLessonIdsAsync(queried, [queriedGraph.PublishedLessonId, otherGraph.PublishedLessonId]),
                    queried,
                    phrase => phrase.LanguageId);
                AssertOnlyTenant(
                    await phrases.FindByLessonIdAsync(queried, queriedGraph.PublishedLessonId),
                    queried,
                    phrase => phrase.LanguageId);
                Assert.Empty(await phrases.FindByIdsAsync(queried, [otherGraph.PhraseId]));
                AssertOnlyTenant(
                    await phrases.FindByIdsAsync(queried, [queriedGraph.PhraseId, otherGraph.PhraseId]),
                    queried,
                    phrase => phrase.LanguageId);

                var variations = new LanguageVariationRepository(db);
                Assert.Null(await variations.FindByIdAsync(queried, otherGraph.VariationId));
                Assert.Empty(await variations.FindByPhraseIdAsync(queried, otherGraph.PhraseId));
                Assert.Empty(await variations.FindByPhraseIdsAsync(queried, [otherGraph.PhraseId]));
                AssertOnlyTenant(
                    await variations.FindByPhraseIdAsync(queried, queriedGraph.PhraseId),
                    queried,
                    variation => variation.LanguageId);
                AssertOnlyTenant(
                    await variations.FindByPhraseIdsAsync(queried, [queriedGraph.PhraseId]),
                    queried,
                    variation => variation.LanguageId);

                Assert.Null(await audio.FindByIdAsync(queried, otherGraph.AudioId));
                Assert.Empty(await audio.FindByPhraseIdAsync(queried, otherGraph.PhraseId));
                Assert.Empty(await audio.FindByPhraseIdsAsync(queried, [otherGraph.PhraseId]));
                Assert.Empty(await audio.FindByVariationIdsAsync(queried, [otherGraph.VariationId]));
                AssertOnlyTenant(
                    await audio.FindPendingReviewAsync(queried, skip: 0, take: 50),
                    queried,
                    recording => recording.LanguageId);
                AssertOnlyTenant(
                    await audio.FindByPhraseIdsAsync(queried, [queriedGraph.PhraseId]),
                    queried,
                    recording => recording.LanguageId);
                AssertOnlyTenant(
                    await audio.FindByVariationIdsAsync(queried, [queriedGraph.VariationId]),
                    queried,
                    recording => recording.LanguageId);

                Assert.Null(await exercises.FindByIdAsync(queried, otherGraph.ExerciseId));
                Assert.Empty(await exercises.FindByLessonIdAsync(queried, otherGraph.PublishedLessonId));
                AssertOnlyTenant(
                    await exercises.FindByLessonIdAsync(queried, queriedGraph.PublishedLessonId),
                    queried,
                    exercise => exercise.LanguageId);

                Assert.Null(await packs.FindByIdAsync(queried, otherGraph.PackId));
                AssertOnlyTenant(await packs.FindAsync(queried, level: null, category: null), queried, pack => pack.LanguageId);

                Assert.Null(await progress.FindByUserAndLessonAsync(queried, otherGraph.LearnerId, otherGraph.PublishedLessonId));
                Assert.Empty(await progress.FindByUserAsync(queried, otherGraph.LearnerId));
                AssertOnlyTenant(
                    await progress.FindByUserAsync(queried, queriedGraph.LearnerId),
                    queried,
                    item => item.LanguageId);
                Assert.Empty(await progress.FindUpdatedSinceAsync(
                    queried,
                    otherGraph.LearnerId,
                    DateTimeOffset.MinValue));
                AssertOnlyTenant(
                    await progress.FindUpdatedSinceAsync(queried, queriedGraph.LearnerId, DateTimeOffset.MinValue),
                    queried,
                    item => item.LanguageId);

                Assert.Null(
                    await srs.FindByUserPhraseAndVariationAsync(
                        queried,
                        otherGraph.LearnerId,
                        otherGraph.PhraseId,
                        variationId: null));
                Assert.Empty(await srs.FindDueAsync(queried, otherGraph.LearnerId, DateOnly.MaxValue));
                AssertOnlyTenant(
                    await srs.FindDueAsync(queried, queriedGraph.LearnerId, DateOnly.MaxValue),
                    queried,
                    record => record.LanguageId);
                Assert.Empty(await srs.FindByUserAsync(queried, otherGraph.LearnerId));
                AssertOnlyTenant(
                    await srs.FindByUserAsync(queried, queriedGraph.LearnerId),
                    queried,
                    record => record.LanguageId);
                Assert.Empty(await srs.FindUpdatedSinceAsync(
                    queried,
                    otherGraph.LearnerId,
                    DateTimeOffset.MinValue));
                AssertOnlyTenant(
                    await srs.FindUpdatedSinceAsync(queried, queriedGraph.LearnerId, DateTimeOffset.MinValue),
                    queried,
                    record => record.LanguageId);

                var receipts = new SyncPushReceiptRepository(db);
                Assert.False(await receipts.ExistsAsync(queried, otherGraph.LearnerId, "op"));

                Assert.Null(await gamification.FindByUserAsync(queried, otherGraph.LearnerId));
                var ownGamification = await gamification.FindByUserAsync(queried, queriedGraph.LearnerId);
                Assert.NotNull(ownGamification);
                Assert.Equal(queried, ownGamification.LanguageId);

                Assert.Null(await requests.FindByIdAsync(queried, otherGraph.RequestId));
                AssertOnlyTenant(
                    await requests.FindOpenSortedByUpvotesAsync(queried, skip: 0, take: 50),
                    queried,
                    request => request.LanguageId);

                Assert.Null(await checkpoints.FindByUserAsync(queried, otherGraph.LearnerId));
                var ownCheckpoint = await checkpoints.FindByUserAsync(queried, queriedGraph.LearnerId);
                Assert.NotNull(ownCheckpoint);
                Assert.Equal(queried, ownCheckpoint.LanguageId);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static void AssertOnlyTenant<T>(
        IReadOnlyList<T> items,
        LanguageId expected,
        Func<T, LanguageId> languageOf)
    {
        Assert.NotEmpty(items);
        Assert.All(items, item => Assert.Equal(expected, languageOf(item)));
    }

    private static async Task<TenantGraph> SeedTenantAsync(KalangaDbContext db, LanguageId languageId, string prefix)
    {
        var now = DateTimeOffset.UtcNow;
        db.Languages.Add(new LanguageRecord
        {
            Id = languageId.Value,
            Code = $"{prefix}{languageId.Value:N}"[..10],
            Name = $"Lang-{prefix}",
            Region = "Test",
            IsActive = true,
            CreatedAt = now,
        });
        await db.SaveChangesAsync();

        var users = new UserRepository(db);
        var lessons = new LessonRepository(db);
        var phrases = new PhraseRepository(db);
        var audio = new AudioRecordingRepository(db);
        var exercises = new ExerciseRepository(db);
        var packs = new ContentPackRepository(db);
        var progress = new LearnerProgressRepository(db);
        var srs = new SpacedRepetitionRepository(db);
        var gamification = new GamificationRepository(db);
        var requests = new RequestRepository(db);
        var checkpoints = new SyncCheckpointRepository(db);

        var learnerEmail = $"{prefix}-learner-{languageId.Value:N}@test.local";
        var learner = User.Register(languageId, learnerEmail, "hash", "Learner", now);
        var contributor = User.Register(languageId, $"{prefix}-contrib-{languageId.Value:N}@test.local", "hash", "Contributor", now);
        contributor.ChangeRole(Role.Contributor, now);
        var reviewer = User.Register(languageId, $"{prefix}-review-{languageId.Value:N}@test.local", "hash", "Reviewer", now);
        reviewer.ChangeRole(Role.Reviewer, now);

        await users.AddAsync(languageId, learner);
        await users.AddAsync(languageId, contributor);
        await users.AddAsync(languageId, reviewer);

        var published = Lesson.CreateDraft(languageId, contributor.Id, $"{prefix} published", Level.Beginner, "Everyday", now);
        published.SubmitForReview(now);
        published.Approve(reviewer.Id, now);
        await lessons.AddAsync(languageId, published);

        var pending = Lesson.CreateDraft(languageId, contributor.Id, $"{prefix} pending", Level.Beginner, "Everyday", now);
        pending.SubmitForReview(now);
        await lessons.AddAsync(languageId, pending);

        var phrase = Phrase.Create(languageId, published.Id, "Ndini", "I am", sortOrder: 0, now);
        await phrases.AddAsync(languageId, phrase);

        var variation = LanguageVariation.Create(languageId, phrase.Id, "Ndini zwino", "everyday", now);
        var variationRepo = new LanguageVariationRepository(db);
        await variationRepo.AddAsync(languageId, variation);

        var recording = AudioRecording.Create(
            languageId,
            contributor.Id,
            "https://cdn.example/a.mp3",
            AudioFileFormat.Mp3,
            fileSizeBytes: 1024,
            durationMs: 800,
            now,
            phrase.Id);
        await audio.AddAsync(languageId, recording);

        var variationAudio = AudioRecording.Create(
            languageId,
            contributor.Id,
            "https://cdn.example/v.mp3",
            AudioFileFormat.Mp3,
            fileSizeBytes: 1024,
            durationMs: 700,
            now,
            phraseId: null,
            variationId: variation.Id);
        await audio.AddAsync(languageId, variationAudio);

        var exercise = Exercise.Create(
            languageId,
            published.Id,
            ExerciseType.Flashcard,
            """{"phrase_id":"00000000-0000-0000-0000-000000000001"}""",
            "I am",
            sortOrder: 0,
            now);
        await exercises.AddAsync(languageId, exercise);

        var pack = ContentPack.Create(
            languageId,
            $"{prefix} pack",
            "https://cdn.example/manifest.json",
            sizeBytes: 10,
            now,
            Level.Beginner,
            "Everyday",
            [published.Id]);
        await packs.AddAsync(languageId, pack);

        var learnerProgress = LearnerProgress.Start(languageId, learner.Id, published.Id, now);
        learnerProgress.Complete(score: 90, xpAwarded: 10, now);
        await progress.AddAsync(languageId, learnerProgress);

        await srs.AddAsync(languageId, SpacedRepetitionRecord.Create(languageId, learner.Id, phrase.Id, now));
        await gamification.AddAsync(languageId, LearnerGamification.Create(languageId, learner.Id, now));

        var request = Request.Submit(languageId, learner.Id, $"{prefix} request", "Need more greetings.", now);
        await requests.AddAsync(languageId, request);
        await checkpoints.AddAsync(languageId, SyncCheckpoint.Create(languageId, learner.Id, now));

        return new TenantGraph(
            learner.Id,
            learnerEmail,
            contributor.Id,
            published.Id,
            phrase.Id,
            variation.Id,
            recording.Id,
            exercise.Id,
            pack.Id,
            request.Id);
    }

    private sealed record TenantGraph(
        UserId LearnerId,
        string LearnerEmail,
        UserId ContributorId,
        LessonId PublishedLessonId,
        PhraseId PhraseId,
        LanguageVariationId VariationId,
        AudioRecordingId AudioId,
        ExerciseId ExerciseId,
        ContentPackId PackId,
        RequestId RequestId);
}
