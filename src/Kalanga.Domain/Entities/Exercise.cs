using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class Exercise
{
    private Exercise(
        ExerciseId id,
        LanguageId languageId,
        LessonId lessonId,
        ExerciseType exerciseType,
        string promptData,
        string correctAnswer,
        int sortOrder,
        DateTimeOffset createdAt)
    {
        Id = id;
        LanguageId = languageId;
        LessonId = lessonId;
        ExerciseType = exerciseType;
        PromptData = promptData;
        CorrectAnswer = correctAnswer;
        SortOrder = sortOrder;
        CreatedAt = createdAt;
    }

    public ExerciseId Id { get; }

    public LanguageId LanguageId { get; }

    public LessonId LessonId { get; }

    public ExerciseType ExerciseType { get; }

    public string PromptData { get; private set; }

    public string CorrectAnswer { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public static Exercise Create(
        LanguageId languageId,
        LessonId lessonId,
        ExerciseType exerciseType,
        string promptData,
        string correctAnswer,
        int sortOrder,
        DateTimeOffset utcNow)
    {
        return new Exercise(
            ExerciseId.New(),
            languageId,
            lessonId,
            exerciseType,
            Guard.RequiredText(promptData, nameof(promptData)),
            Guard.RequiredText(correctAnswer, nameof(correctAnswer)),
            Guard.NonNegative(sortOrder, nameof(sortOrder)),
            utcNow);
    }
}
