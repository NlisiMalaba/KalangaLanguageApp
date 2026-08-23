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
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class PlatformMetricsConsistencyPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 42: Platform Metrics Consistency
    [Fact(Timeout = 180_000)]
    public async Task Platform_metrics_match_database_counts()
    {
        var catalogShape =
            from extraUsers in Gen.Int[0, 5]
            from publishedLessons in Gen.Int[0, 4]
            from draftLessons in Gen.Int[0, 3]
            from approvedAudio in Gen.Int[0, 4]
            from pendingAudio in Gen.Int[0, 3]
            select (extraUsers, publishedLessons, draftLessons, approvedAudio, pendingAudio);

        await Check.SampleAsync(
            catalogShape,
            async (
                int extraUsers,
                int publishedLessons,
                int draftLessons,
                int approvedAudio,
                int pendingAudio) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);

                var users = new UserRepository(db);
                var lessons = new LessonRepository(db);
                var phrases = new PhraseRepository(db);
                var audio = new AudioRecordingRepository(db);
                var now = DateTimeOffset.UtcNow;

                var admin = User.Register(
                    languageId,
                    $"admin-{Guid.NewGuid():N}@example.com",
                    "hash",
                    "Admin",
                    now);
                admin.ChangeRole(Role.Admin, now);
                await users.AddAsync(languageId, admin);

                var contributor = User.Register(
                    languageId,
                    $"contrib-{Guid.NewGuid():N}@example.com",
                    "hash",
                    "Contributor",
                    now);
                contributor.ChangeRole(Role.Contributor, now);
                await users.AddAsync(languageId, contributor);

                var reviewer = User.Register(
                    languageId,
                    $"review-{Guid.NewGuid():N}@example.com",
                    "hash",
                    "Reviewer",
                    now);
                reviewer.ChangeRole(Role.Reviewer, now);
                await users.AddAsync(languageId, reviewer);

                for (var i = 0; i < extraUsers; i++)
                {
                    await users.AddAsync(
                        languageId,
                        User.Register(
                            languageId,
                            $"learner-{i}-{Guid.NewGuid():N}@example.com",
                            "hash",
                            $"Learner{i}",
                            now));
                }

                Lesson? firstPublished = null;
                for (var i = 0; i < publishedLessons; i++)
                {
                    var lesson = Lesson.CreateDraft(
                        languageId,
                        contributor.Id,
                        $"Published {i}",
                        Level.Beginner,
                        "Everyday",
                        now);
                    lesson.SubmitForReview(now);
                    lesson.Approve(reviewer.Id, now);
                    await lessons.AddAsync(languageId, lesson);
                    firstPublished ??= lesson;
                }

                for (var i = 0; i < draftLessons; i++)
                {
                    await lessons.AddAsync(
                        languageId,
                        Lesson.CreateDraft(
                            languageId,
                            contributor.Id,
                            $"Draft {i}",
                            Level.Beginner,
                            "Everyday",
                            now));
                }

                // Audio requires a phrase (and thus a lesson). Seed one published lesson if needed.
                if (approvedAudio + pendingAudio > 0 && firstPublished is null)
                {
                    firstPublished = Lesson.CreateDraft(
                        languageId,
                        contributor.Id,
                        "Audio host",
                        Level.Beginner,
                        "Everyday",
                        now);
                    firstPublished.SubmitForReview(now);
                    firstPublished.Approve(reviewer.Id, now);
                    await lessons.AddAsync(languageId, firstPublished);
                }

                if (firstPublished is not null && approvedAudio + pendingAudio > 0)
                {
                    var phrase = Phrase.Create(
                        languageId,
                        firstPublished.Id,
                        "Ndini",
                        "I am",
                        sortOrder: 0,
                        now);
                    await phrases.AddAsync(languageId, phrase);

                    for (var i = 0; i < approvedAudio; i++)
                    {
                        var recording = AudioRecording.Create(
                            languageId,
                            contributor.Id,
                            $"https://cdn.example/a-{i}.mp3",
                            AudioFileFormat.Mp3,
                            fileSizeBytes: 1024,
                            durationMs: 800,
                            now,
                            phrase.Id);
                        recording.Approve();
                        await audio.AddAsync(languageId, recording);
                    }

                    for (var i = 0; i < pendingAudio; i++)
                    {
                        await audio.AddAsync(
                            languageId,
                            AudioRecording.Create(
                                languageId,
                                contributor.Id,
                                $"https://cdn.example/p-{i}.mp3",
                                AudioFileFormat.Mp3,
                                fileSizeBytes: 1024,
                                durationMs: 800,
                                now,
                                phrase.Id));
                    }
                }

                var expectedUsers = await db.Users.CountAsync(user => user.LanguageId == languageId.Value);
                var expectedPublished = await db.Lessons.CountAsync(lesson =>
                    lesson.LanguageId == languageId.Value
                    && lesson.Status == LessonStatus.Published.ToString());
                var expectedApprovedAudio = await db.AudioRecordings.CountAsync(recording =>
                    recording.LanguageId == languageId.Value
                    && recording.Status == AudioRecordingStatus.Approved.ToString());

                var useCase = new GetPlatformMetricsUseCase(users, new PlatformMetricsReader(db));
                var metrics = await useCase.ExecuteAsync(
                    new GetPlatformMetricsCommand(languageId, admin.Id));

                Assert.Equal(expectedUsers, metrics.TotalUsers);
                Assert.Equal(expectedPublished, metrics.TotalPublishedLessons);
                Assert.Equal(expectedApprovedAudio, metrics.TotalApprovedAudioRecordings);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
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
