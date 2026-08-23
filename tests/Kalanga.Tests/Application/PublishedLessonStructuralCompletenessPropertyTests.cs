using System.Text.Json;
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
public sealed class PublishedLessonStructuralCompletenessPropertyTests(PostgresFixture postgres)
{
    private static readonly string[] Categories = ["Everyday", "Travel", "Family", "Work"];
    private static readonly string[] Registers = ["formal", "everyday"];

    // Feature: kalanga-language-app, Property 7: Published Lesson Structural Completeness
    [Fact(Timeout = 180_000)]
    public async Task Get_published_lesson_returns_complete_phrase_variation_audio_and_exercise_graph()
    {
        var text = Gen.String[Gen.Char['a', 'z'], 2, 24];

        var variationSpec =
            from kalanga in text
            from register in Gen.OneOfConst(Registers)
            from approvedAudio in Gen.Bool
            select new VariationSpec(kalanga, register, approvedAudio);

        var phraseSpec =
            from kalanga in text
            from english in text
            from variations in variationSpec.Array[0, 2]
            from extraPendingAudio in Gen.Bool
            select new PhraseSpec(kalanga, english, variations, extraPendingAudio);

        var exerciseSpec =
            from type in Gen.Enum<ExerciseType>()
            from prompt in text
            from answer in text
            select new ExerciseSpec(type, prompt, answer);

        var lessonGraph =
            from level in Gen.Enum<Level>()
            from category in Gen.OneOfConst(Categories)
            from isScenario in Gen.Bool
            from scenarioContext in text
            from phrases in phraseSpec.Array[1, 3]
            from exercises in exerciseSpec.Array[1, 3]
            select new LessonGraphSpec(
                level,
                category,
                isScenario,
                isScenario ? scenarioContext : null,
                phrases,
                exercises);

        await Check.SampleAsync(
            lessonGraph,
            async (LessonGraphSpec spec) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);

                var now = DateTimeOffset.UtcNow;
                var users = new UserRepository(db);
                var contributor = await SeedContributorAsync(users, languageId, now);
                var reviewer = await SeedReviewerAsync(users, languageId, now);

                var lessons = new LessonRepository(db);
                var phrases = new PhraseRepository(db);
                var variations = new LanguageVariationRepository(db);
                var audio = new AudioRecordingRepository(db);
                var exercises = new ExerciseRepository(db);

                var lesson = Lesson.CreateDraft(
                    languageId,
                    contributor,
                    "Published completeness",
                    spec.Level,
                    spec.Category,
                    now,
                    spec.IsScenario,
                    spec.ScenarioContext);
                lesson.SubmitForReview(now);
                lesson.Approve(reviewer, now);
                await lessons.AddAsync(languageId, lesson);

                var expectedPhrases = new List<ExpectedPhrase>(spec.Phrases.Length);
                var pendingAudioIds = new HashSet<AudioRecordingId>();

                for (var phraseIndex = 0; phraseIndex < spec.Phrases.Length; phraseIndex++)
                {
                    var phraseSpecItem = spec.Phrases[phraseIndex];
                    var createdAt = now.AddMinutes(phraseIndex);
                    var phrase = Phrase.Create(
                        languageId,
                        lesson.Id,
                        phraseSpecItem.KalangaText,
                        phraseSpecItem.EnglishTranslation,
                        phraseIndex,
                        createdAt);
                    await phrases.AddAsync(languageId, phrase);

                    var approvedPhraseAudio = await AddAudioAsync(
                        audio,
                        languageId,
                        contributor,
                        createdAt,
                        approve: true,
                        phrase.Id,
                        variationId: null);
                    if (phraseSpecItem.ExtraPendingAudio)
                    {
                        var pending = await AddAudioAsync(
                            audio,
                            languageId,
                            contributor,
                            createdAt.AddSeconds(1),
                            approve: false,
                            phrase.Id,
                            variationId: null);
                        pendingAudioIds.Add(pending.Id);
                    }

                    var expectedVariations = new List<ExpectedVariation>(phraseSpecItem.Variations.Length);
                    for (var variationIndex = 0; variationIndex < phraseSpecItem.Variations.Length; variationIndex++)
                    {
                        var variationSpecItem = phraseSpecItem.Variations[variationIndex];
                        var variation = LanguageVariation.Create(
                            languageId,
                            phrase.Id,
                            variationSpecItem.KalangaText,
                            variationSpecItem.RegisterLabel,
                            createdAt.AddSeconds(variationIndex + 2));
                        await variations.AddAsync(languageId, variation);

                        AudioRecording? variationAudio = null;
                        if (variationSpecItem.ApprovedAudio)
                        {
                            variationAudio = await AddAudioAsync(
                                audio,
                                languageId,
                                contributor,
                                createdAt.AddSeconds(variationIndex + 10),
                                approve: true,
                                phraseId: null,
                                variation.Id);
                        }

                        expectedVariations.Add(new ExpectedVariation(
                            variation.Id,
                            variation.KalangaText,
                            variation.RegisterLabel,
                            variationAudio?.Id));
                    }

                    expectedPhrases.Add(new ExpectedPhrase(
                        phrase.Id,
                        phrase.KalangaText,
                        phrase.EnglishTranslation,
                        phrase.SortOrder,
                        approvedPhraseAudio.Id,
                        expectedVariations));
                }

                var expectedExercises = new List<ExpectedExercise>(spec.Exercises.Length);
                for (var exerciseIndex = 0; exerciseIndex < spec.Exercises.Length; exerciseIndex++)
                {
                    var exerciseSpecItem = spec.Exercises[exerciseIndex];
                    var exercise = Exercise.Create(
                        languageId,
                        lesson.Id,
                        exerciseSpecItem.ExerciseType,
                        System.Text.Json.JsonSerializer.Serialize(new { prompt = exerciseSpecItem.PromptData }),
                        exerciseSpecItem.CorrectAnswer,
                        exerciseIndex,
                        now.AddMinutes(exerciseIndex));
                    await exercises.AddAsync(languageId, exercise);
                    expectedExercises.Add(new ExpectedExercise(
                        exercise.Id,
                        exercise.ExerciseType,
                        exercise.PromptData,
                        exercise.CorrectAnswer,
                        exercise.SortOrder));
                }

                var getLesson = new GetLessonUseCase(lessons, phrases, variations, audio, exercises);
                var result = await getLesson.ExecuteAsync(new GetLessonCommand(languageId, lesson.Id));
                var dto = result.Lesson;

                Assert.Equal(lesson.Id, dto.LessonId);
                Assert.Equal(languageId, dto.LanguageId);
                Assert.Equal(lesson.Title, dto.Title);
                Assert.Equal(spec.Level, dto.Level);
                Assert.Equal(spec.Category, dto.Category);
                Assert.Equal(spec.IsScenario, dto.IsScenario);
                Assert.Equal(spec.ScenarioContext, dto.ScenarioContext);
                Assert.NotEmpty(dto.Phrases);
                Assert.NotEmpty(dto.Exercises);
                Assert.Equal(expectedPhrases.Count, dto.Phrases.Count);
                Assert.Equal(expectedExercises.Count, dto.Exercises.Count);

                for (var i = 0; i < expectedPhrases.Count; i++)
                {
                    var expected = expectedPhrases[i];
                    var actual = dto.Phrases[i];

                    Assert.False(string.IsNullOrWhiteSpace(actual.KalangaText));
                    Assert.False(string.IsNullOrWhiteSpace(actual.EnglishTranslation));
                    Assert.Equal(expected.PhraseId, actual.PhraseId);
                    Assert.Equal(expected.KalangaText, actual.KalangaText);
                    Assert.Equal(expected.EnglishTranslation, actual.EnglishTranslation);
                    Assert.Equal(expected.SortOrder, actual.SortOrder);

                    var phraseAudio = Assert.Single(actual.Audio);
                    Assert.Equal(expected.ApprovedAudioId, phraseAudio.AudioRecordingId);
                    Assert.False(string.IsNullOrWhiteSpace(phraseAudio.CdnUrl));
                    Assert.DoesNotContain(actual.Audio, item => pendingAudioIds.Contains(item.AudioRecordingId));

                    Assert.Equal(expected.Variations.Count, actual.Variations.Count);
                    for (var v = 0; v < expected.Variations.Count; v++)
                    {
                        var expectedVariation = expected.Variations[v];
                        var actualVariation = actual.Variations[v];
                        Assert.Equal(expectedVariation.VariationId, actualVariation.VariationId);
                        Assert.Equal(expectedVariation.KalangaText, actualVariation.KalangaText);
                        Assert.Equal(expectedVariation.RegisterLabel, actualVariation.RegisterLabel);
                        Assert.False(string.IsNullOrWhiteSpace(actualVariation.KalangaText));
                        Assert.False(string.IsNullOrWhiteSpace(actualVariation.RegisterLabel));

                        if (expectedVariation.ApprovedAudioId is { } variationAudioId)
                        {
                            Assert.Equal(variationAudioId, Assert.Single(actualVariation.Audio).AudioRecordingId);
                        }
                        else
                        {
                            Assert.Empty(actualVariation.Audio);
                        }
                    }
                }

                for (var i = 0; i < expectedExercises.Count; i++)
                {
                    var expected = expectedExercises[i];
                    var actual = dto.Exercises[i];
                    Assert.Equal(expected.ExerciseId, actual.ExerciseId);
                    Assert.Equal(expected.ExerciseType, actual.ExerciseType);
                    AssertJsonEqual(expected.PromptData, actual.PromptData);
                    Assert.Equal(expected.CorrectAnswer, actual.CorrectAnswer);
                    Assert.Equal(expected.SortOrder, actual.SortOrder);
                    Assert.False(string.IsNullOrWhiteSpace(actual.PromptData));
                    Assert.False(string.IsNullOrWhiteSpace(actual.CorrectAnswer));
                }

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static async Task<AudioRecording> AddAudioAsync(
        AudioRecordingRepository audio,
        LanguageId languageId,
        UserId contributorId,
        DateTimeOffset createdAt,
        bool approve,
        PhraseId? phraseId,
        LanguageVariationId? variationId)
    {
        var recording = AudioRecording.Create(
            languageId,
            contributorId,
            $"https://cdn.example/{Guid.NewGuid():N}.mp3",
            AudioFileFormat.Mp3,
            fileSizeBytes: 2048,
            durationMs: 800,
            createdAt,
            phraseId,
            variationId);
        if (approve)
        {
            recording.Approve();
        }

        await audio.AddAsync(languageId, recording);
        return recording;
    }

    private static async Task<UserId> SeedContributorAsync(UserRepository users, LanguageId languageId, DateTimeOffset now)
    {
        var contributor = User.Register(
            languageId,
            $"contrib-{Guid.NewGuid():N}@example.com",
            "hash",
            "Contributor",
            now);
        contributor.ChangeRole(Role.Contributor, now);
        await users.AddAsync(languageId, contributor);
        return contributor.Id;
    }

    private static async Task<UserId> SeedReviewerAsync(UserRepository users, LanguageId languageId, DateTimeOffset now)
    {
        var reviewer = User.Register(
            languageId,
            $"review-{Guid.NewGuid():N}@example.com",
            "hash",
            "Reviewer",
            now);
        reviewer.ChangeRole(Role.Reviewer, now);
        await users.AddAsync(languageId, reviewer);
        return reviewer.Id;
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

    private static void AssertJsonEqual(string expected, string actual)
    {
        using var expectedDoc = JsonDocument.Parse(expected);
        using var actualDoc = JsonDocument.Parse(actual);
        Assert.True(
            JsonElement.DeepEquals(expectedDoc.RootElement, actualDoc.RootElement),
            $"JSON differed.{Environment.NewLine}Expected: {expected}{Environment.NewLine}Actual: {actual}");
    }

    private sealed record VariationSpec(string KalangaText, string RegisterLabel, bool ApprovedAudio);

    private sealed record PhraseSpec(
        string KalangaText,
        string EnglishTranslation,
        VariationSpec[] Variations,
        bool ExtraPendingAudio);

    private sealed record ExerciseSpec(ExerciseType ExerciseType, string PromptData, string CorrectAnswer);

    private sealed record LessonGraphSpec(
        Level Level,
        string Category,
        bool IsScenario,
        string? ScenarioContext,
        PhraseSpec[] Phrases,
        ExerciseSpec[] Exercises);

    private sealed record ExpectedVariation(
        LanguageVariationId VariationId,
        string KalangaText,
        string RegisterLabel,
        AudioRecordingId? ApprovedAudioId);

    private sealed record ExpectedPhrase(
        PhraseId PhraseId,
        string KalangaText,
        string EnglishTranslation,
        int SortOrder,
        AudioRecordingId ApprovedAudioId,
        IReadOnlyList<ExpectedVariation> Variations);

    private sealed record ExpectedExercise(
        ExerciseId ExerciseId,
        ExerciseType ExerciseType,
        string PromptData,
        string CorrectAnswer,
        int SortOrder);
}
