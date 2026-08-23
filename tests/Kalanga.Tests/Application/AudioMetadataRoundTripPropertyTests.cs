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
public sealed class AudioMetadataRoundTripPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 35: Audio Metadata Round-Trip
    [Fact(Timeout = 180_000)]
    public async Task Uploaded_audio_metadata_is_persisted_exactly()
    {
        var metadata =
            from isMp3 in Gen.Bool
            from gender in Gen.Enum<SpeakerGender>()
            from hasDialect in Gen.Bool
            from dialect in Gen.String[Gen.Char['a', 'z'], 3, 24]
            from durationMs in Gen.Int[1, 60_000]
            select new MetadataSample(isMp3, gender, hasDialect ? dialect : null, durationMs);

        await Check.SampleAsync(
            metadata,
            async (MetadataSample sample) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);
                var now = DateTimeOffset.UtcNow;
                var users = new UserRepository(db);
                var contributor = await SeedContributorAsync(users, languageId, now);
                var lessons = new LessonRepository(db);
                var lesson = Lesson.CreateDraft(languageId, contributor, "Greetings", Level.Beginner, "Everyday", now);
                await lessons.AddAsync(languageId, lesson);
                var phrase = Phrase.Create(languageId, lesson.Id, "Ndini", "I am", 0, now);
                await new PhraseRepository(db).AddAsync(languageId, phrase);

                var recordings = new AudioRecordingRepository(db);
                var storage = new FakeAudioStorage();
                var useCase = new UploadAudioUseCase(
                    users,
                    new PhraseRepository(db),
                    new LanguageVariationRepository(db),
                    lessons,
                    recordings,
                    storage);

                await using var content = new MemoryStream(sample.IsMp3 ? Mp3Bytes() : AacBytes());
                var result = await useCase.ExecuteAsync(new UploadAudioCommand(
                    languageId,
                    contributor,
                    content,
                    content.Length,
                    sample.DurationMs,
                    phrase.Id,
                    VariationId: null,
                    sample.Gender,
                    sample.DialectLabel));

                Assert.Equal(sample.IsMp3 ? AudioFileFormat.Mp3 : AudioFileFormat.Aac, result.FileFormat);
                Assert.Equal(sample.Gender, result.SpeakerGender);
                Assert.Equal(sample.DialectLabel, result.DialectLabel);
                Assert.Equal(sample.DurationMs, result.DurationMs);
                Assert.Equal(phrase.Id, result.PhraseId);

                var persisted = await recordings.FindByIdAsync(languageId, result.AudioRecordingId);
                Assert.NotNull(persisted);
                Assert.Equal(result.CdnUrl, persisted.CdnUrl);
                Assert.Equal(result.FileFormat, persisted.FileFormat);
                Assert.Equal(result.FileSizeBytes, persisted.FileSizeBytes);
                Assert.Equal(sample.Gender, persisted.SpeakerGender);
                Assert.Equal(sample.DialectLabel, persisted.DialectLabel);
                Assert.Equal(sample.DurationMs, persisted.DurationMs);
                Assert.Equal(phrase.Id, persisted.PhraseId);
                Assert.Equal(contributor, persisted.ContributorId);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static byte[] Mp3Bytes() => "ID3"u8.ToArray().Concat(Enumerable.Repeat((byte)0x22, 40)).ToArray();

    private static byte[] AacBytes() => new byte[] { 0xFF, 0xF1 }.Concat(Enumerable.Repeat((byte)0x33, 40)).ToArray();

    private static async Task<UserId> SeedContributorAsync(UserRepository users, LanguageId languageId, DateTimeOffset now)
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

    private sealed record MetadataSample(bool IsMp3, SpeakerGender Gender, string? DialectLabel, int DurationMs);

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
