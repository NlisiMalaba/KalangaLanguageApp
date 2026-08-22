using Kalanga.Application.Audio;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.Out;
using Kalanga.Application.UseCases;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class UploadAudioUseCaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Upload_stores_pending_review_recording_with_cdn_url_and_metadata()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, contributor, phrase, lessons, users) = await SeedPhraseAsync(db);
        var storage = new FakeAudioStorage();
        var audio = new AudioRecordingRepository(db);
        var useCase = new UploadAudioUseCase(
            users,
            new PhraseRepository(db),
            new LanguageVariationRepository(db),
            lessons,
            audio,
            storage);

        await using var content = Mp3Stream();
        var result = await useCase.ExecuteAsync(new UploadAudioCommand(
            languageId,
            contributor,
            content,
            DeclaredContentLength: content.Length,
            DurationMs: 1500,
            phrase.Id,
            VariationId: null,
            SpeakerGender.Female,
            "Western"));

        Assert.Equal(AudioRecordingStatus.PendingReview, result.Status);
        Assert.Equal(AudioFileFormat.Mp3, result.FileFormat);
        Assert.Equal(SpeakerGender.Female, result.SpeakerGender);
        Assert.Equal("Western", result.DialectLabel);
        Assert.Equal(phrase.Id, result.PhraseId);
        Assert.StartsWith("https://cdn.test/", result.CdnUrl);
        Assert.Contains($"audio/{languageId.Value}/{phrase.Id.Value}/{result.AudioRecordingId.Value}.mp3", result.CdnUrl);

        var persisted = await audio.FindByIdAsync(languageId, result.AudioRecordingId);
        Assert.NotNull(persisted);
        Assert.Equal(AudioRecordingStatus.PendingReview, persisted.Status);
        Assert.Equal(result.CdnUrl, persisted.CdnUrl);
        Assert.Single(storage.Puts);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Invalid_format_is_rejected_without_storing()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, contributor, phrase, lessons, users) = await SeedPhraseAsync(db);
        var storage = new FakeAudioStorage();
        var useCase = new UploadAudioUseCase(
            users,
            new PhraseRepository(db),
            new LanguageVariationRepository(db),
            lessons,
            new AudioRecordingRepository(db),
            storage);

        await using var content = new MemoryStream("GIF89a-not-audio"u8.ToArray());
        await Assert.ThrowsAsync<InvalidAudioUploadException>(() =>
            useCase.ExecuteAsync(new UploadAudioCommand(
                languageId,
                contributor,
                content,
                content.Length,
                DurationMs: 800,
                phrase.Id,
                VariationId: null)));

        Assert.Empty(storage.Puts);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Learner_cannot_upload_audio()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, _, phrase, lessons, users) = await SeedPhraseAsync(db);
        var learner = await SeedUserAsync(users, languageId, Role.Learner, DateTimeOffset.UtcNow, "l");
        var useCase = new UploadAudioUseCase(
            users,
            new PhraseRepository(db),
            new LanguageVariationRepository(db),
            lessons,
            new AudioRecordingRepository(db),
            new FakeAudioStorage());

        await using var content = Mp3Stream();
        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            useCase.ExecuteAsync(new UploadAudioCommand(
                languageId,
                learner,
                content,
                content.Length,
                DurationMs: 800,
                phrase.Id,
                VariationId: null)));

        await transaction.RollbackAsync();
    }

    private static MemoryStream Mp3Stream()
    {
        var bytes = "ID3"u8.ToArray().Concat(Enumerable.Repeat((byte)0x11, 64)).ToArray();
        return new MemoryStream(bytes);
    }

    private static async Task<(LanguageId LanguageId, UserId Contributor, Phrase Phrase, LessonRepository Lessons, UserRepository Users)> SeedPhraseAsync(
        KalangaDbContext db)
    {
        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now, "c");
        var lessons = new LessonRepository(db);
        var lesson = Lesson.CreateDraft(languageId, contributor, "Greetings", Level.Beginner, "Everyday", now);
        await lessons.AddAsync(languageId, lesson);
        var phrase = Phrase.Create(languageId, lesson.Id, "Ndini", "I am", 0, now);
        await new PhraseRepository(db).AddAsync(languageId, phrase);
        return (languageId, contributor, phrase, lessons, users);
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
        if (role != Role.Learner)
        {
            user.ChangeRole(role, now);
        }

        await users.AddAsync(languageId, user);
        return user.Id;
    }

    private static async Task SeedLanguageAsync(KalangaDbContext db, LanguageId languageId)
    {
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
    }

    private sealed class FakeAudioStorage : IAudioStorage
    {
        public List<string> Puts { get; } = [];

        public Task<string> PutAsync(
            LanguageId languageId,
            PhraseId phraseId,
            AudioRecordingId recordingId,
            AudioFileFormat format,
            Stream content,
            int contentLength,
            CancellationToken cancellationToken = default)
        {
            var key = AudioObjectKeys.For(languageId, phraseId, recordingId, format);
            Puts.Add(key);
            return Task.FromResult($"https://cdn.test/{key}");
        }

        public Task TryDeleteAsync(
            LanguageId languageId,
            PhraseId phraseId,
            AudioRecordingId recordingId,
            AudioFileFormat format,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
