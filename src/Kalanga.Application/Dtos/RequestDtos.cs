using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public sealed record RequestDto(
    RequestId RequestId,
    LanguageId LanguageId,
    UserId SubmitterId,
    string Title,
    string Description,
    int UpvoteCount,
    RequestStatus Status,
    LessonId? FulfilledByLessonId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SubmitRequestCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    string Title,
    string Description);

public sealed record SubmitRequestResult(RequestDto Request);

public sealed record ListRequestsCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    int Skip,
    int Take);

public sealed record ListRequestsResult(IReadOnlyList<RequestDto> Requests);

public sealed record UpvoteRequestCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    RequestId RequestId);

public sealed record UpvoteRequestResult(RequestDto Request, bool Applied);

public sealed record FulfillRequestCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    RequestId RequestId,
    LessonId LessonId);

public sealed record FulfillRequestResult(RequestDto Request);

public static class RequestNotificationTypes
{
    public const string RequestFulfilled = "request_fulfilled";
}
