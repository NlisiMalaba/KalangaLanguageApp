using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public sealed record BrowseLessonCatalogCommand(
    LanguageId LanguageId,
    Level? Level,
    string? Category,
    int Skip,
    int Take);

public sealed record LessonCatalogItemDto(
    LessonId LessonId,
    LanguageId LanguageId,
    string Title,
    Level Level,
    string Category,
    bool IsScenario,
    string? ScenarioContext,
    int XpReward,
    DateTimeOffset UpdatedAt);

public sealed record BrowseLessonCatalogResult(IReadOnlyList<LessonCatalogItemDto> Lessons);

public sealed record GetLessonCommand(LanguageId LanguageId, LessonId LessonId);

public sealed record AudioRefDto(
    AudioRecordingId AudioRecordingId,
    string CdnUrl,
    AudioFileFormat FileFormat,
    SpeakerGender SpeakerGender,
    string? DialectLabel,
    int DurationMs);

public sealed record LanguageVariationDto(
    LanguageVariationId VariationId,
    string KalangaText,
    string RegisterLabel,
    IReadOnlyList<AudioRefDto> Audio);

public sealed record PhraseDetailDto(
    PhraseId PhraseId,
    string KalangaText,
    string EnglishTranslation,
    int SortOrder,
    IReadOnlyList<LanguageVariationDto> Variations,
    IReadOnlyList<AudioRefDto> Audio);

public sealed record ExerciseDetailDto(
    ExerciseId ExerciseId,
    ExerciseType ExerciseType,
    string PromptData,
    string CorrectAnswer,
    int SortOrder);

public sealed record LessonDetailDto(
    LessonId LessonId,
    LanguageId LanguageId,
    string Title,
    Level Level,
    string Category,
    bool IsScenario,
    string? ScenarioContext,
    int XpReward,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<PhraseDetailDto> Phrases,
    IReadOnlyList<ExerciseDetailDto> Exercises);

public sealed record GetLessonResult(LessonDetailDto Lesson);
