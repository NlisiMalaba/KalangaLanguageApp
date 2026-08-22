using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public enum ReviewLessonAction
{
    Approve = 0,
    Reject = 1,
    RequestRevision = 2,
}

public enum AdminLessonOverrideAction
{
    Publish = 0,
    Unpublish = 1,
}

public sealed record ListReviewQueueCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    int Skip,
    int Take);

public sealed record ReviewQueueItemDto(
    LessonId LessonId,
    LanguageId LanguageId,
    string Title,
    Level Level,
    string Category,
    UserId ContributorId,
    DateTimeOffset UpdatedAt);

public sealed record ListReviewQueueResult(IReadOnlyList<ReviewQueueItemDto> Lessons);

public sealed record ReviewLessonCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    LessonId LessonId,
    ReviewLessonAction Action,
    string? Feedback);

public sealed record ReviewLessonResult(
    LessonId LessonId,
    LanguageId LanguageId,
    LessonStatus Status,
    UserId? ReviewedBy,
    string? ReviewFeedback);

public sealed record OverrideLessonPublicationCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    LessonId LessonId,
    AdminLessonOverrideAction Action);

public sealed record OverrideLessonPublicationResult(
    LessonId LessonId,
    LanguageId LanguageId,
    LessonStatus Status);

public static class ReviewNotificationTypes
{
    public const string LessonRejected = "lesson_rejected";
    public const string LessonRevisionRequested = "lesson_revision_requested";
}
