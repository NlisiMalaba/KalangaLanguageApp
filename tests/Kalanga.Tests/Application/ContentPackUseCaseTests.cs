using Kalanga.Application;
using Kalanga.Application.Dtos;
using Kalanga.Application.UseCases;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Infrastructure.Security;
using Kalanga.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class ContentPackUseCaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task List_returns_tenant_packs_with_size_bytes_and_rejects_other_language()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var packs = new ContentPackRepository(db);
        var beginner = ContentPack.Create(
            languageId,
            "Beginner Everyday",
            "https://cdn.example/manifest.json",
            sizeBytes: 4096,
            now,
            Level.Beginner,
            "Everyday");
        var travel = ContentPack.Create(
            languageId,
            "Beginner Travel",
            "https://cdn.example/travel.json",
            sizeBytes: 1024,
            now,
            Level.Beginner,
            "Travel");
        await packs.AddAsync(languageId, beginner);
        await packs.AddAsync(languageId, travel);

        var listed = await new ListContentPacksUseCase(packs).ExecuteAsync(
            new ListContentPacksCommand(languageId, Level.Beginner, "Everyday"));

        Assert.Single(listed.Packs);
        Assert.Equal(beginner.Id, listed.Packs[0].PackId);
        Assert.Equal(4096, listed.Packs[0].SizeBytes);

        await Assert.ThrowsAsync<TenantAccessDeniedException>(() =>
            new ListContentPacksUseCase(packs).ExecuteAsync(
                new ListContentPacksCommand(languageId, Level: null, Category: null, LanguageId.New())));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Manifest_includes_published_lesson_ids_approved_cdn_urls_and_hmac()
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
        var phrases = new PhraseRepository(db);
        var variations = new LanguageVariationRepository(db);
        var audio = new AudioRecordingRepository(db);
        var packs = new ContentPackRepository(db);

        var published = Lesson.CreateDraft(languageId, contributor, "Greetings", Level.Beginner, "Everyday", now);
        published.SubmitForReview(now);
        published.Approve(reviewer, now);
        await lessons.AddAsync(languageId, published);

        var draft = Lesson.CreateDraft(languageId, contributor, "Hidden", Level.Beginner, "Everyday", now);
        await lessons.AddAsync(languageId, draft);

        var phrase = Phrase.Create(languageId, published.Id, "Ndini", "I am", 0, now);
        await phrases.AddAsync(languageId, phrase);
        var variation = LanguageVariation.Create(languageId, phrase.Id, "Ndini zwino", "everyday", now);
        await variations.AddAsync(languageId, variation);

        var approved = AudioRecording.Create(
            languageId,
            contributor,
            "https://cdn.example/approved.mp3",
            AudioFileFormat.Mp3,
            fileSizeBytes: 2048,
            durationMs: 900,
            now,
            phrase.Id);
        approved.Approve();
        await audio.AddAsync(languageId, approved);

        var pending = AudioRecording.Create(
            languageId,
            contributor,
            "https://cdn.example/pending.mp3",
            AudioFileFormat.Mp3,
            fileSizeBytes: 1024,
            durationMs: 800,
            now,
            phrase.Id);
        await audio.AddAsync(languageId, pending);

        var variationAudio = AudioRecording.Create(
            languageId,
            contributor,
            "https://cdn.example/variation.aac",
            AudioFileFormat.Aac,
            fileSizeBytes: 512,
            durationMs: 700,
            now,
            phraseId: null,
            variation.Id);
        variationAudio.Approve();
        await audio.AddAsync(languageId, variationAudio);

        var pack = ContentPack.Create(
            languageId,
            "Beginner Everyday",
            "https://cdn.example/manifest.json",
            sizeBytes: 10,
            now,
            Level.Beginner,
            "Everyday",
            [published.Id, draft.Id]);
        await packs.AddAsync(languageId, pack);

        var generatedAt = new DateTimeOffset(2026, 8, 22, 15, 0, 0, TimeSpan.Zero);
        var signer = new HmacContentPackManifestSigner(Options.Create(new ContentPacksOptions
        {
            SigningKey = "DEV-ONLY-content-pack-hmac-key-32b!",
        }));
        var useCase = new GetContentPackManifestUseCase(
            packs,
            lessons,
            phrases,
            variations,
            audio,
            signer,
            new FrozenTimeProvider(generatedAt));

        var result = await useCase.ExecuteAsync(new GetContentPackManifestCommand(languageId, pack.Id));

        Assert.Equal(pack.Id, result.PackId);
        Assert.Equal("HMAC-SHA256", result.Algorithm);
        Assert.Single(result.Lessons);
        Assert.Equal(published.Id, result.Lessons[0].LessonId);
        Assert.Equal(2, result.Lessons[0].Audio.Count);
        Assert.Contains(result.Lessons[0].Audio, item => item.CdnUrl == "https://cdn.example/approved.mp3");
        Assert.Contains(result.Lessons[0].Audio, item => item.CdnUrl == "https://cdn.example/variation.aac");
        Assert.DoesNotContain(result.Lessons[0].Audio, item => item.CdnUrl.Contains("pending"));
        Assert.Equal(2560, result.SizeBytes);

        var expected = signer.Sign(ContentPackManifestCanonical.Write(result with { Signature = string.Empty }));
        Assert.Equal(expected, result.Signature);

        await Assert.ThrowsAsync<ContentPackNotFoundException>(() =>
            useCase.ExecuteAsync(new GetContentPackManifestCommand(languageId, ContentPackId.New())));

        await transaction.RollbackAsync();
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

    private sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
