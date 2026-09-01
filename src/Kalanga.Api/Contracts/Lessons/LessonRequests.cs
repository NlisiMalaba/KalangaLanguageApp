using Kalanga.Domain.Enums;

namespace Kalanga.Api.Contracts.Lessons;

public sealed record CreateLessonRequest(
    string Title,
    Level Level,
    string Category,
    bool IsScenario = false,
    string? ScenarioContext = null,
    int? XpReward = null);

public sealed record UpdateLessonRequest(
    string Title,
    Level Level,
    string Category,
    bool IsScenario = false,
    string? ScenarioContext = null,
    int? XpReward = null,
    IReadOnlyList<DraftPhraseRequest>? Phrases = null,
    IReadOnlyList<DraftExerciseRequest>? Exercises = null);

public sealed record DraftVariationRequest(Guid? VariationId, string KalangaText, string RegisterLabel);

public sealed record DraftPhraseRequest(
    Guid? PhraseId,
    string KalangaText,
    string EnglishTranslation,
    int SortOrder,
    IReadOnlyList<DraftVariationRequest>? Variations = null);

public sealed record DraftExerciseRequest(
    Guid? ExerciseId,
    ExerciseType ExerciseType,
    string PromptData,
    string CorrectAnswer,
    int SortOrder);
