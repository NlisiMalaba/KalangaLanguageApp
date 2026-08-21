using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = nameof(Role.Admin))]
public sealed class ListUsersUseCase(IUserRepository users) : ListUsersPort
{
    public async Task<ListUsersResult> ExecuteAsync(
        ListUsersCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.Role != Role.Admin)
        {
            throw new UnauthorizedRoleException("list users", Role.Admin);
        }

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var listed = await users.ListAsync(command.LanguageId, command.Skip, command.Take, cancellationToken);
        var dtos = listed
            .Select(user => new AdminUserDto(
                user.Id,
                user.LanguageId,
                user.Email,
                user.DisplayName,
                user.Role,
                user.Status,
                user.CreatedAt,
                user.UpdatedAt))
            .ToList();

        return new ListUsersResult(dtos);
    }
}
