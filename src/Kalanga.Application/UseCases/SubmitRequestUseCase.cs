using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class SubmitRequestUseCase(
    IUserRepository users,
    IRequestRepository requests,
    TimeProvider? time = null) : SubmitRequestPort
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public async Task<SubmitRequestResult> ExecuteAsync(
        SubmitRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var request = Request.Submit(
            command.LanguageId,
            actor.Id,
            command.Title,
            command.Description,
            _time.GetUtcNow());

        await requests.AddAsync(command.LanguageId, request, cancellationToken);
        return new SubmitRequestResult(request.ToDto());
    }
}
