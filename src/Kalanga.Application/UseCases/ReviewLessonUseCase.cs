using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = $"{nameof(Role.Reviewer)},{nameof(Role.Admin)}")]
public sealed class ReviewLessonUseCase(
    IUserRepository users,
    ILessonRepository lessons,
    INotificationOutbox notifications,
    ICatalogCache? catalogCache = null) : ReviewLessonPort
{
    public async Task<ReviewLessonResult> ExecuteAsync(
        ReviewLessonCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        LessonReviewGuard.EnsureCanReview(actor);

        var lesson = await lessons.FindByIdAsync(command.LanguageId, command.LessonId, cancellationToken)
            ?? throw new LessonNotFoundException(command.LessonId);

        var utcNow = DateTimeOffset.UtcNow;

        switch (command.Action)
        {
            case ReviewLessonAction.Approve:
                lesson.Approve(actor.Id, utcNow);
                break;

            case ReviewLessonAction.Reject:
                lesson.Reject(actor.Id, RequireFeedback(command.Feedback), utcNow);
                break;

            case ReviewLessonAction.RequestRevision:
                lesson.RequestRevision(actor.Id, RequireFeedback(command.Feedback), utcNow);
                break;

            default:
                throw new InvalidReviewException($"Unsupported review action '{command.Action}'.");
        }

        await lessons.UpdateAsync(command.LanguageId, lesson, cancellationToken);

        if (command.Action == ReviewLessonAction.Approve)
        {
            catalogCache?.Invalidate(command.LanguageId);
        }

        if (command.Action is ReviewLessonAction.Reject or ReviewLessonAction.RequestRevision)
        {
            var notificationType = command.Action == ReviewLessonAction.Reject
                ? ReviewNotificationTypes.LessonRejected
                : ReviewNotificationTypes.LessonRevisionRequested;

            await notifications.EnqueueReviewFeedbackAsync(
                command.LanguageId,
                lesson.ContributorId,
                lesson.Id,
                notificationType,
                lesson.ReviewFeedback!,
                cancellationToken);
        }

        return new ReviewLessonResult(
            lesson.Id,
            lesson.LanguageId,
            lesson.Status,
            lesson.ReviewedBy,
            lesson.ReviewFeedback);
    }

    private static string RequireFeedback(string? feedback)
    {
        if (string.IsNullOrWhiteSpace(feedback))
        {
            throw new InvalidReviewException("Review feedback is required.");
        }

        return feedback;
    }
}
