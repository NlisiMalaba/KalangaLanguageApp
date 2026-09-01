using Kalanga.Domain;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public sealed record CreateLessonCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    string Title,
    Level Level,
    string Category,
    bool IsScenario = false,
    string? ScenarioContext = null,
    int? XpReward = null);

public sealed record CreateLessonResult(
    LessonId LessonId,
    LanguageId LanguageId,
    string Title,
    Level Level,
    string Category,
    bool IsScenario,
    string? ScenarioContext,
    LessonStatus Status,
    UserId ContributorId,
    int XpReward);

public sealed record SubmitLessonForReviewCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    LessonId LessonId);

public sealed record SubmitLessonForReviewResult(
    LessonId LessonId,
    LanguageId LanguageId,
    LessonStatus Status);

public sealed record DraftVariationItem(Guid? VariationId, string KalangaText, string RegisterLabel);

public sealed record DraftPhraseItem(
    Guid? PhraseId,
    string KalangaText,
    string EnglishTranslation,
    int SortOrder,
    IReadOnlyList<DraftVariationItem> Variations);

public sealed record DraftExerciseItem(
    Guid? ExerciseId,
    ExerciseType ExerciseType,
    string PromptData,
    string CorrectAnswer,
    int SortOrder);

public sealed record SaveLessonDraftCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    LessonId LessonId,
    string Title,
    Level Level,
    string Category,
    bool IsScenario = false,
    string? ScenarioContext = null,
    int XpReward = DomainRules.DefaultLessonXpReward,
    IReadOnlyList<DraftPhraseItem>? Phrases = null,
    IReadOnlyList<DraftExerciseItem>? Exercises = null);

public sealed record SaveLessonDraftResult(
    LessonId LessonId,
    LanguageId LanguageId,
    string Title,
    Level Level,
    string Category,
    bool IsScenario,
    string? ScenarioContext,
    LessonStatus Status,
    UserId ContributorId,
    int XpReward);
