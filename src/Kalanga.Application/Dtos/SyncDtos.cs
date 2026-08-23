using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public sealed record SyncProgressItemDto(
    LessonId LessonId,
    DateTimeOffset? CompletedAt,
    int? Score,
    int XpAwarded,
    DateTimeOffset UpdatedAt);

public sealed record SyncSrsItemDto(
    PhraseId PhraseId,
    LanguageVariationId? VariationId,
    decimal EaseFactor,
    int IntervalDays,
    int Repetitions,
    DateOnly NextReviewAt,
    DateTimeOffset? LastReviewedAt,
    DateTimeOffset UpdatedAt);

public sealed record SyncGamificationDto(
    int TotalXp,
    int CurrentStreak,
    int LongestStreak,
    DateOnly? LastActivityDate,
    Level ProgressLevel,
    int XpDelta,
    DateTimeOffset UpdatedAt);

public sealed record SyncChangesDto(
    IReadOnlyList<SyncProgressItemDto> Progress,
    IReadOnlyList<SyncSrsItemDto> SpacedRepetition,
    SyncGamificationDto? Gamification);

public sealed record SyncProgressCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    string ClientOperationId,
    IReadOnlyList<SyncProgressItemDto> Progress,
    IReadOnlyList<SyncSrsItemDto> SpacedRepetition,
    SyncGamificationDto? Gamification);

public sealed record SyncProgressResult(
    long SyncVersion,
    DateTimeOffset LastSyncedAt,
    bool IdempotentReplay,
    SyncChangesDto ServerChanges);

public sealed record PullSyncCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    long SinceVersion);

public sealed record PullSyncResult(
    long SyncVersion,
    DateTimeOffset LastSyncedAt,
    SyncChangesDto ServerChanges);
