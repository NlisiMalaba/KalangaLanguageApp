using Kalanga.Application.Dtos;
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
public sealed class GetAudioUseCaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Approved_audio_returns_cdn_metadata_without_bytes()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, contributor, learner, recording, users, audio) = await SeedAsync(db, approve: true);
        var result = await new GetAudioUseCase(users, audio).ExecuteAsync(
            new GetAudioCommand(languageId, learner, recording.Id));

        Assert.Equal(recording.Id, result.AudioRecordingId);
        Assert.Equal(recording.CdnUrl, result.CdnUrl);
        Assert.Equal(AudioRecordingStatus.Approved, result.Status);
        Assert.Equal(AudioFileFormat.Mp3, result.FileFormat);
        Assert.DoesNotContain("ID3", result.CdnUrl, StringComparison.Ordinal);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Learner_cannot_see_pending_audio()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, _, learner, recording, users, audio) = await SeedAsync(db, approve: false);

        await Assert.ThrowsAsync<AudioRecordingNotFoundException>(() =>
            new GetAudioUseCase(users, audio).ExecuteAsync(
                new GetAudioCommand(languageId, learner, recording.Id)));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Contributor_can_see_own_pending_audio()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, contributor, _, recording, users, audio) = await SeedAsync(db, approve: false);
        var result = await new GetAudioUseCase(users, audio).ExecuteAsync(
            new GetAudioCommand(languageId, contributor, recording.Id));

        Assert.Equal(AudioRecordingStatus.PendingReview, result.Status);
        Assert.Equal(recording.CdnUrl, result.CdnUrl);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Missing_recording_is_not_found()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, _, learner, _, users, audio) = await SeedAsync(db, approve: true);

        await Assert.ThrowsAsync<AudioRecordingNotFoundException>(() =>
            new GetAudioUseCase(users, audio).ExecuteAsync(
                new GetAudioCommand(languageId, learner, AudioRecordingId.New())));

        await transaction.RollbackAsync();
    }

    private static async Task<(
        LanguageId LanguageId,
        UserId Contributor,
        UserId Learner,
        AudioRecording Recording,
        UserRepository Users,
        AudioRecordingRepository Audio)> SeedAsync(KalangaDbContext db, bool approve)
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
        var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now, "c");
        var learner = await SeedUserAsync(users, languageId, Role.Learner, now, "l");
        var lessons = new LessonRepository(db);
        var lesson = Lesson.CreateDraft(languageId, contributor, "Greetings", Level.Beginner, "Everyday", now);
        await lessons.AddAsync(languageId, lesson);
        var phrase = Phrase.Create(languageId, lesson.Id, "Ndini", "I am", 0, now);
        await new PhraseRepository(db).AddAsync(languageId, phrase);

        var recording = AudioRecording.Create(
            languageId,
            contributor,
            "https://cdn.test/audio/a.mp3",
            AudioFileFormat.Mp3,
            fileSizeBytes: 2048,
            durationMs: 900,
            now,
            phrase.Id);
        if (approve)
        {
            recording.Approve();
        }

        var audio = new AudioRecordingRepository(db);
        await audio.AddAsync(languageId, recording);
        return (languageId, contributor, learner, recording, users, audio);
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
}
