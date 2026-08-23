using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Entities;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class NotificationOutbox(KalangaDbContext db) : INotificationOutbox
{
    public async Task EnqueueReviewFeedbackAsync(
        LanguageId languageId,
        UserId recipientUserId,
        LessonId lessonId,
        string notificationType,
        string feedback,
        CancellationToken cancellationToken = default)
    {
        db.NotificationOutbox.Add(new NotificationOutboxRecord
        {
            Id = Guid.CreateVersion7(),
            LanguageId = languageId.Value,
            RecipientUserId = recipientUserId.Value,
            LessonId = lessonId.Value,
            NotificationType = notificationType,
            Payload = feedback,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task EnqueueRequestFulfilledAsync(
        LanguageId languageId,
        UserId recipientUserId,
        LessonId lessonId,
        RequestId requestId,
        string title,
        CancellationToken cancellationToken = default)
    {
        db.NotificationOutbox.Add(new NotificationOutboxRecord
        {
            Id = Guid.CreateVersion7(),
            LanguageId = languageId.Value,
            RecipientUserId = recipientUserId.Value,
            LessonId = lessonId.Value,
            NotificationType = RequestNotificationTypes.RequestFulfilled,
            Payload = $"{requestId.Value:D}\n{title}",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
