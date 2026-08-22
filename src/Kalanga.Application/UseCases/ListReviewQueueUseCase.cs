using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = $"{nameof(Role.Reviewer)},{nameof(Role.Admin)}")]
public sealed class ListReviewQueueUseCase(
    IUserRepository users,
    ILessonRepository lessons) : ListReviewQueuePort
{
    public async Task<ListReviewQueueResult> ExecuteAsync(
        ListReviewQueueCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        LessonReviewGuard.EnsureCanReview(actor);

        var pending = await lessons.FindPendingReviewAsync(
            command.LanguageId,
            command.Skip,
            command.Take,
            cancellationToken);

        var items = pending
            .Select(static lesson => new ReviewQueueItemDto(
                lesson.Id,
                lesson.LanguageId,
                lesson.Title,
                lesson.Level,
                lesson.Category,
                lesson.ContributorId,
                lesson.UpdatedAt))
            .ToList();

        return new ListReviewQueueResult(items);
    }
}
