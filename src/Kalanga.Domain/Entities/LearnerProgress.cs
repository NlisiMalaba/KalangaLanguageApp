using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class LearnerProgress
{
    internal LearnerProgress(
        LearnerProgressId id,
        LanguageId languageId,
        UserId userId,
        LessonId lessonId,
        DateTimeOffset? completedAt,
        int? score,
        int xpAwarded,
        DateTimeOffset updatedAt)
    {
        Id = id;
        LanguageId = languageId;
        UserId = userId;
        LessonId = lessonId;
        CompletedAt = completedAt;
        Score = score;
        XpAwarded = xpAwarded;
        UpdatedAt = updatedAt;
    }

    public LearnerProgressId Id { get; }

    public LanguageId LanguageId { get; }

    public UserId UserId { get; }

    public LessonId LessonId { get; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public int? Score { get; private set; }

    public int XpAwarded { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsCompleted => CompletedAt is not null;

    public static LearnerProgress Start(LanguageId languageId, UserId userId, LessonId lessonId, DateTimeOffset utcNow)
    {
        return new LearnerProgress(
            LearnerProgressId.New(),
            languageId,
            userId,
            lessonId,
            completedAt: null,
            score: null,
            xpAwarded: 0,
            utcNow);
    }

    public void Complete(int score, int xpAwarded, DateTimeOffset utcNow)
    {
        Guard.NonNegative(score, nameof(score));
        Guard.NonNegative(xpAwarded, nameof(xpAwarded));
        CompletedAt = utcNow;
        Score = score;
        XpAwarded = xpAwarded;
        UpdatedAt = utcNow;
    }

    public static LearnerProgress FromClient(
        LanguageId languageId,
        UserId userId,
        LessonId lessonId,
        DateTimeOffset? completedAt,
        int? score,
        int xpAwarded,
        DateTimeOffset updatedAt)
    {
        if (score is { } value)
        {
            Guard.NonNegative(value, nameof(score));
        }

        Guard.NonNegative(xpAwarded, nameof(xpAwarded));
        return new LearnerProgress(
            LearnerProgressId.New(),
            languageId,
            userId,
            lessonId,
            completedAt,
            score,
            xpAwarded,
            updatedAt);
    }

    public bool ApplyLastWriteWins(
        DateTimeOffset? completedAt,
        int? score,
        int xpAwarded,
        DateTimeOffset incomingUpdatedAt)
    {
        if (incomingUpdatedAt <= UpdatedAt)
        {
            return false;
        }

        if (score is { } value)
        {
            Guard.NonNegative(value, nameof(score));
        }

        Guard.NonNegative(xpAwarded, nameof(xpAwarded));
        CompletedAt = completedAt;
        Score = score;
        XpAwarded = xpAwarded;
        UpdatedAt = incomingUpdatedAt;
        return true;
    }
}
