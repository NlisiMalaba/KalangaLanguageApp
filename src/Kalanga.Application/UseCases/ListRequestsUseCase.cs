using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class ListRequestsUseCase(
    IUserRepository users,
    IRequestRepository requests) : ListRequestsPort
{
    public async Task<ListRequestsResult> ExecuteAsync(
        ListRequestsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var items = await requests.FindOpenSortedByUpvotesAsync(
            command.LanguageId,
            command.Skip,
            command.Take,
            cancellationToken);

        return new ListRequestsResult(items.Select(static request => request.ToDto()).ToList());
    }
}
