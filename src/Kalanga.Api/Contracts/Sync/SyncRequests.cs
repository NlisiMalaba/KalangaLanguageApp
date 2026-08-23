namespace Kalanga.Api.Contracts.Sync;

public sealed record SyncPushRequest(
    string ClientOperationId,
    IReadOnlyList<SyncProgressItemRequest> Progress,
    IReadOnlyList<SyncSrsItemRequest> SpacedRepetition,
    SyncGamificationRequest? Gamification);

public sealed record SyncProgressItemRequest(
    Guid LessonId,
    DateTimeOffset? CompletedAt,
    int? Score,
    int XpAwarded,
    DateTimeOffset UpdatedAt);

public sealed record SyncSrsItemRequest(
    Guid PhraseId,
    Guid? VariationId,
    decimal EaseFactor,
    int IntervalDays,
    int Repetitions,
    DateOnly NextReviewAt,
    DateTimeOffset? LastReviewedAt,
    DateTimeOffset UpdatedAt);

public sealed record SyncGamificationRequest(
    int TotalXp,
    int CurrentStreak,
    int LongestStreak,
    DateOnly? LastActivityDate,
    int XpDelta,
    DateTimeOffset UpdatedAt);
