using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public sealed record CompleteLessonCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    LessonId LessonId,
    int Score);

public sealed record CompleteLessonResult(
    LessonId LessonId,
    DateTimeOffset CompletedAt,
    int Score,
    int XpAwarded,
    int TotalXp,
    bool XpGranted);

public sealed record GetProgressCommand(LanguageId LanguageId, UserId ActorUserId);

public sealed record ProgressBucketDto(
    Level? Level,
    string? Category,
    int CompletedCount,
    int TotalCount,
    decimal Percentage);

public sealed record WeakAreaDto(
    Level Level,
    string Category,
    decimal AverageScore,
    int SampleSize);

public sealed record GetProgressResult(
    UserId UserId,
    int TotalXp,
    IReadOnlyList<ProgressBucketDto> ByLevel,
    IReadOnlyList<ProgressBucketDto> ByCategory,
    IReadOnlyList<WeakAreaDto> WeakAreas);
