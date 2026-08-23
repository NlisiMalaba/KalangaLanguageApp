using CsCheck;
using Kalanga.Application.Audio;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.Out;
using Kalanga.Application.UseCases;
using Kalanga.Domain;
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
public sealed class AudioUploadValidationInvariantPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 34: Audio Upload Validation Invariant
    [Fact(Timeout = 180_000)]
    public async Task Invalid_audio_is_rejected_and_valid_mp3_or_aac_is_accepted()
    {
        var payload =
            from kind in Gen.Int[0, 4]
            from padding in Gen.Byte.Array[8, 48]
            select (kind, padding);

        await Check.SampleAsync(
            payload,
            async (int kind, byte[] padding) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var (languageId, contributor, phrase, lessons, users) = await SeedPhraseAsync(db);
                var storage = new FakeAudioStorage();
                var recordings = new AudioRecordingRepository(db);
                var useCase = new UploadAudioUseCase(
                    users,
                    new PhraseRepository(db),
                    new LanguageVariationRepository(db),
                    lessons,
                    recordings,
                    storage);

                await using var content = new MemoryStream(BuildBytes(kind, padding));
                var declaredLength = kind == 4
                    ? DomainRules.MaxAudioFileSizeBytes + 1L
                    : content.Length;

                var command = new UploadAudioCommand(
                    languageId,
                    contributor,
                    content,
                    declaredLength,
                    DurationMs: 900,
                    phrase.Id,
                    VariationId: null);

                if (kind is 0 or 1)
                {
                    var result = await useCase.ExecuteAsync(command);
                    Assert.Equal(kind == 0 ? AudioFileFormat.Mp3 : AudioFileFormat.Aac, result.FileFormat);
                    Assert.Equal(AudioRecordingStatus.PendingReview, result.Status);
                    Assert.Single(storage.Puts);
                }
                else
                {
                    await Assert.ThrowsAsync<InvalidAudioUploadException>(() => useCase.ExecuteAsync(command));
                    Assert.Empty(storage.Puts);
                    Assert.Empty(await recordings.FindByPhraseIdAsync(languageId, phrase.Id));
                }

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static byte[] BuildBytes(int kind, byte[] padding) =>
        kind switch
        {
            0 => "ID3"u8.ToArray().Concat(padding).ToArray(),
            1 => new byte[] { 0xFF, 0xF1 }.Concat(padding).ToArray(),
            2 => "GIF"u8.ToArray().Concat(padding).ToArray(),
            3 => [],
            4 => "ID3"u8.ToArray().Concat(padding).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown payload kind."),
        };

    private static async Task<(LanguageId LanguageId, UserId Contributor, Phrase Phrase, LessonRepository Lessons, UserRepository Users)> SeedPhraseAsync(
        KalangaDbContext db)
    {
        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var contributor = await SeedUserAsync(users, languageId, now);
        var lessons = new LessonRepository(db);
        var lesson = Lesson.CreateDraft(languageId, contributor, "Greetings", Level.Beginner, "Everyday", now);
        await lessons.AddAsync(languageId, lesson);
        var phrase = Phrase.Create(languageId, lesson.Id, "Ndini", "I am", 0, now);
        await new PhraseRepository(db).AddAsync(languageId, phrase);
        return (languageId, contributor, phrase, lessons, users);
    }

    private static async Task<UserId> SeedUserAsync(UserRepository users, LanguageId languageId, DateTimeOffset now)
    {
        var user = User.Register(
            languageId,
            $"contrib-{Guid.NewGuid():N}@example.com",
            "hash",
            "Contributor",
            now);
        user.ChangeRole(Role.Contributor, now);
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
            Puts.Add(AudioObjectKeys.For(languageId, phraseId, recordingId, format));
            return Task.FromResult($"https://cdn.test/{Puts[^1]}");
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
