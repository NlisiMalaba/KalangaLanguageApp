using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = $"{nameof(Role.Contributor)},{nameof(Role.Admin)}")]
public sealed class FulfillRequestUseCase(
    IUserRepository users,
    IRequestRepository requests,
    ILessonRepository lessons,
    INotificationOutbox notifications,
    IUnitOfWork unitOfWork,
    TimeProvider? time = null) : FulfillRequestPort
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public async Task<FulfillRequestResult> ExecuteAsync(
        FulfillRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        LessonAuthoringGuard.EnsureCanAuthor(actor, "fulfill a request");

        var request = await requests.FindByIdAsync(command.LanguageId, command.RequestId, cancellationToken)
            ?? throw new RequestNotFoundException(command.RequestId);

        var lesson = await lessons.FindByIdAsync(command.LanguageId, command.LessonId, cancellationToken)
            ?? throw new LessonNotFoundException(command.LessonId);

        var now = _time.GetUtcNow();
        request.Fulfill(lesson.Id, now);

        await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                await requests.UpdateAsync(command.LanguageId, request, ct);
                await notifications.EnqueueRequestFulfilledAsync(
                    command.LanguageId,
                    request.SubmitterId,
                    lesson.Id,
                    request.Id,
                    request.Title,
                    ct);
                return true;
            },
            cancellationToken);

        return new FulfillRequestResult(request.ToDto());
    }
}
