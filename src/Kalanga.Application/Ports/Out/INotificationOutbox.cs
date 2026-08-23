using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface INotificationOutbox
{
    Task EnqueueReviewFeedbackAsync(
        LanguageId languageId,
        UserId recipientUserId,
        LessonId lessonId,
        string notificationType,
        string feedback,
        CancellationToken cancellationToken = default);

    Task EnqueueRequestFulfilledAsync(
        LanguageId languageId,
        UserId recipientUserId,
        LessonId lessonId,
        RequestId requestId,
        string title,
        CancellationToken cancellationToken = default);
}
