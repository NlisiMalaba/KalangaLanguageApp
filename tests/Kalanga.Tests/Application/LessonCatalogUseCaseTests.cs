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
public sealed class LessonCatalogUseCaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Browse_returns_only_published_lessons_for_the_tenant_and_filters()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        var otherLanguageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        await SeedLanguageAsync(db, otherLanguageId);

        var now = DateTimeOffset.UtcNow;
        var contributor = await SeedContributorAsync(db, languageId, now);
        var otherContributor = await SeedContributorAsync(db, otherLanguageId, now);
        var reviewer = await SeedReviewerAsync(db, languageId, now);

        var lessons = new LessonRepository(db);
        var match = await SeedPublishedLessonAsync(
            lessons, languageId, contributor, reviewer, "Greetings", Level.Beginner, "Everyday", now);
        await SeedPublishedLessonAsync(
            lessons, languageId, contributor, reviewer, "Travel", Level.Intermediate, "Travel", now);
        await SeedPublishedLessonAsync(
            lessons, otherLanguageId, otherContributor, reviewer, "Other greetings", Level.Beginner, "Everyday", now);

        var draft = Lesson.CreateDraft(languageId, contributor, "Draft greetings", Level.Beginner, "Everyday", now);
        await lessons.AddAsync(languageId, draft);

        var pending = Lesson.CreateDraft(languageId, contributor, "Pending greetings", Level.Beginner, "Everyday", now);
        pending.SubmitForReview(now);
        await lessons.AddAsync(languageId, pending);

        var unpublished = Lesson.CreateDraft(languageId, contributor, "Was live", Level.Beginner, "Everyday", now);
        unpublished.SubmitForReview(now);
        unpublished.Approve(reviewer, now);
        unpublished.Unpublish(now);
        await lessons.AddAsync(languageId, unpublished);

        var browse = new BrowseLessonCatalogUseCase(lessons);
        var result = await browse.ExecuteAsync(
            new BrowseLessonCatalogCommand(languageId, Level.Beginner, "Everyday", Skip: 0, Take: 50));

        var ids = result.Lessons.Select(lesson => lesson.LessonId).ToArray();
        Assert.Equal([match.Id], ids);
        Assert.All(result.Lessons, item =>
        {
            Assert.Equal(languageId, item.LanguageId);
            Assert.Equal(Level.Beginner, item.Level);
            Assert.Equal("Everyday", item.Category);
        });

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Get_lesson_returns_published_content_graph_and_hides_unpublished_audio()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);

        var now = DateTimeOffset.UtcNow;
        var contributor = await SeedContributorAsync(db, languageId, now);
        var reviewer = await SeedReviewerAsync(db, languageId, now);

        var lessons = new LessonRepository(db);
        var phrases = new PhraseRepository(db);
        var variations = new LanguageVariationRepository(db);
        var audio = new AudioRecordingRepository(db);
        var exercises = new ExerciseRepository(db);

        var lesson = await SeedPublishedLessonAsync(
            lessons, languageId, contributor, reviewer, "Greetings", Level.Beginner, "Everyday", now);

        var phrase = Phrase.Create(languageId, lesson.Id, "Ndini", "I am", sortOrder: 0, now);
        await phrases.AddAsync(languageId, phrase);

        var variation = LanguageVariation.Create(languageId, phrase.Id, "Ndini zwino", "everyday", now);
        await variations.AddAsync(languageId, variation);

        var approvedPhraseAudio = AudioRecording.Create(
            languageId, contributor, "https://cdn.example/phrase.mp3", AudioFileFormat.Mp3, 2048, 900, now, phrase.Id);
        approvedPhraseAudio.Approve();
        await audio.AddAsync(languageId, approvedPhraseAudio);

        var pendingPhraseAudio = AudioRecording.Create(
            languageId, contributor, "https://cdn.example/pending.mp3", AudioFileFormat.Mp3, 2048, 900, now, phrase.Id);
        await audio.AddAsync(languageId, pendingPhraseAudio);

        var approvedVariationAudio = AudioRecording.Create(
            languageId,
            contributor,
            "https://cdn.example/variation.mp3",
            AudioFileFormat.Mp3,
            2048,
            800,
            now,
            phraseId: null,
            variationId: variation.Id);
        approvedVariationAudio.Approve();
        await audio.AddAsync(languageId, approvedVariationAudio);

        var exercise = Exercise.Create(
            languageId,
            lesson.Id,
            ExerciseType.Flashcard,
            """{"front":"Ndini"}""",
            "I am",
            sortOrder: 0,
            now);
        await exercises.AddAsync(languageId, exercise);

        var getLesson = new GetLessonUseCase(lessons, phrases, variations, audio, exercises);
        var result = await getLesson.ExecuteAsync(new GetLessonCommand(languageId, lesson.Id));

        Assert.Equal(lesson.Id, result.Lesson.LessonId);
        Assert.Equal("Greetings", result.Lesson.Title);
        var phraseDto = Assert.Single(result.Lesson.Phrases);
        Assert.Equal("Ndini", phraseDto.KalangaText);
        Assert.Equal("I am", phraseDto.EnglishTranslation);
        Assert.Equal(approvedPhraseAudio.Id, Assert.Single(phraseDto.Audio).AudioRecordingId);

        var variationDto = Assert.Single(phraseDto.Variations);
        Assert.Equal("everyday", variationDto.RegisterLabel);
        Assert.Equal(approvedVariationAudio.Id, Assert.Single(variationDto.Audio).AudioRecordingId);

        var exerciseDto = Assert.Single(result.Lesson.Exercises);
        Assert.Equal(ExerciseType.Flashcard, exerciseDto.ExerciseType);
        Assert.Equal("I am", exerciseDto.CorrectAnswer);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Get_lesson_treats_non_published_lessons_as_not_found()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);

        var now = DateTimeOffset.UtcNow;
        var contributor = await SeedContributorAsync(db, languageId, now);
        var lessons = new LessonRepository(db);
        var draft = Lesson.CreateDraft(languageId, contributor, "Not live", Level.Beginner, "Everyday", now);
        await lessons.AddAsync(languageId, draft);

        var getLesson = new GetLessonUseCase(
            lessons,
            new PhraseRepository(db),
            new LanguageVariationRepository(db),
            new AudioRecordingRepository(db),
            new ExerciseRepository(db));

        await Assert.ThrowsAsync<LessonNotFoundException>(() =>
            getLesson.ExecuteAsync(new GetLessonCommand(languageId, draft.Id)));

        await transaction.RollbackAsync();
    }

    private static async Task<UserId> SeedContributorAsync(KalangaDbContext db, LanguageId languageId, DateTimeOffset now)
    {
        var contributor = User.Register(
            languageId,
            $"contrib-{Guid.NewGuid():N}@example.com",
            "hash",
            "Contributor",
            now);
        contributor.ChangeRole(Role.Contributor, now);
        await new UserRepository(db).AddAsync(languageId, contributor);
        return contributor.Id;
    }

    private static async Task<UserId> SeedReviewerAsync(KalangaDbContext db, LanguageId languageId, DateTimeOffset now)
    {
        var reviewer = User.Register(
            languageId,
            $"review-{Guid.NewGuid():N}@example.com",
            "hash",
            "Reviewer",
            now);
        reviewer.ChangeRole(Role.Reviewer, now);
        await new UserRepository(db).AddAsync(languageId, reviewer);
        return reviewer.Id;
    }

    private static async Task<Lesson> SeedPublishedLessonAsync(
        LessonRepository lessons,
        LanguageId languageId,
        UserId contributorId,
        UserId reviewerId,
        string title,
        Level level,
        string category,
        DateTimeOffset now)
    {
        var lesson = Lesson.CreateDraft(languageId, contributorId, title, level, category, now);
        lesson.SubmitForReview(now);
        lesson.Approve(reviewerId, now);
        await lessons.AddAsync(languageId, lesson);
        return lesson;
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
