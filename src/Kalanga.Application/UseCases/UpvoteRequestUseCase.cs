using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class UpvoteRequestUseCase(
    IUserRepository users,
    IRequestRepository requests) : UpvoteRequestPort
{
    public async Task<UpvoteRequestResult> ExecuteAsync(
        UpvoteRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var request = await requests.FindByIdAsync(command.LanguageId, command.RequestId, cancellationToken)
            ?? throw new RequestNotFoundException(command.RequestId);

        if (request.Status != RequestStatus.Open)
        {
            throw new InvalidRequestStateException("A fulfilled request cannot be modified.");
        }

        var applied = await requests.AddUpvoteAsync(
            command.LanguageId,
            request.Id,
            actor.Id,
            cancellationToken);

        if (applied)
        {
            request = await requests.FindByIdAsync(command.LanguageId, request.Id, cancellationToken)
                ?? throw new RequestNotFoundException(command.RequestId);
        }

        return new UpvoteRequestResult(request.ToDto(), applied);
    }
}
