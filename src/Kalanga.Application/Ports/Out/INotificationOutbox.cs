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
}
