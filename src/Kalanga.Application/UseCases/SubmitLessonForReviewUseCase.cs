using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = $"{nameof(Role.Contributor)},{nameof(Role.Admin)}")]
public sealed class SubmitLessonForReviewUseCase(
    IUserRepository users,
    ILessonRepository lessons) : SubmitLessonForReviewPort
{
    public async Task<SubmitLessonForReviewResult> ExecuteAsync(
        SubmitLessonForReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        var lesson = await lessons.FindByIdAsync(command.LanguageId, command.LessonId, cancellationToken)
            ?? throw new LessonNotFoundException(command.LessonId);

        LessonAuthoringGuard.EnsureCanMutate(actor, lesson, "submit for review");

        lesson.SubmitForReview(DateTimeOffset.UtcNow);
        await lessons.UpdateAsync(command.LanguageId, lesson, cancellationToken);

        return new SubmitLessonForReviewResult(lesson.Id, lesson.LanguageId, lesson.Status);
    }
}
