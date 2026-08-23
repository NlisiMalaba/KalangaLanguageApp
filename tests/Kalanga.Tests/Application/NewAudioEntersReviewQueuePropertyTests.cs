using CsCheck;
using Kalanga.Application.Audio;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.Out;
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
public sealed class NewAudioEntersReviewQueuePropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 39: New Audio for Published Phrase Enters Review Queue
    [Fact(Timeout = 180_000)]
    public async Task Audio_uploaded_for_a_published_phrase_is_pending_review_and_not_playable()
    {
        var input =
            from durationMs in Gen.Int[1, 20_000]
            from gender in Gen.Enum<SpeakerGender>()
            select (durationMs, gender);

        await Check.SampleAsync(
            input,
            async (int durationMs, SpeakerGender gender) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);
                var now = DateTimeOffset.UtcNow;
                var users = new UserRepository(db);
                var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now, "c");
                var reviewer = await SeedUserAsync(users, languageId, Role.Reviewer, now, "r");
                var lessons = new LessonRepository(db);

                var lesson = Lesson.CreateDraft(languageId, contributor, "Published greetings", Level.Beginner, "Everyday", now);
                lesson.SubmitForReview(now);
                lesson.Approve(reviewer, now);
                await lessons.AddAsync(languageId, lesson);

                var phrase = Phrase.Create(languageId, lesson.Id, "Ndini", "I am", 0, now);
                await new PhraseRepository(db).AddAsync(languageId, phrase);

                var recordings = new AudioRecordingRepository(db);
                var useCase = new UploadAudioUseCase(
                    users,
                    new PhraseRepository(db),
                    new LanguageVariationRepository(db),
                    lessons,
                    recordings,
                    new FakeAudioStorage());

                await using var content = new MemoryStream("ID3"u8.ToArray().Concat(Enumerable.Repeat((byte)0x44, 48)).ToArray());
                var result = await useCase.ExecuteAsync(new UploadAudioCommand(
                    languageId,
                    contributor,
                    content,
                    content.Length,
                    durationMs,
                    phrase.Id,
                    VariationId: null,
                    gender));

                Assert.Equal(AudioRecordingStatus.PendingReview, result.Status);
                Assert.False(result.Status == AudioRecordingStatus.Approved);

                var persisted = await recordings.FindByIdAsync(languageId, result.AudioRecordingId);
                Assert.NotNull(persisted);
                Assert.Equal(AudioRecordingStatus.PendingReview, persisted.Status);
                Assert.False(persisted.IsPlayableByLearners);

                var queue = await recordings.FindPendingReviewAsync(languageId, skip: 0, take: 50);
                Assert.Contains(queue, item => item.Id == result.AudioRecordingId);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static async Task<UserId> SeedUserAsync(
        UserRepository users,
        LanguageId languageId,
        Role role,
        DateTimeOffset now,
        string suffix)
    {
        var user = User.Register(
            languageId,
            $"{role.ToString().ToLowerInvariant()}-{suffix}-{Guid.NewGuid():N}@example.com",
            "hash",
            role.ToString(),
            now);
        user.ChangeRole(role, now);
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

    private sealed class FakeAudioStorage : IAudioStorage
    {
        public Task<string> PutAsync(
            LanguageId languageId,
            PhraseId phraseId,
            AudioRecordingId recordingId,
            AudioFileFormat format,
            Stream content,
            int contentLength,
            CancellationToken cancellationToken = default) =>
            Task.FromResult($"https://cdn.test/{AudioObjectKeys.For(languageId, phraseId, recordingId, format)}");

        public Task TryDeleteAsync(
            LanguageId languageId,
            PhraseId phraseId,
            AudioRecordingId recordingId,
            AudioFileFormat format,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
