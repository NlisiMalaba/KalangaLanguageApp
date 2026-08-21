using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = nameof(Role.Admin))]
public sealed class ManageUserRoleUseCase(IUserRepository users) : ManageUserRolePort
{
    public async Task<ManageUserRoleResult> ExecuteAsync(
        ManageUserRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.Role != Role.Admin)
        {
            throw new UnauthorizedRoleException("manage users", Role.Admin);
        }

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var target = await users.FindByIdAsync(command.LanguageId, command.TargetUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.TargetUserId);

        var utcNow = DateTimeOffset.UtcNow;

        switch (command.Action)
        {
            case ManageUserAction.ChangeRole:
                if (command.NewRole is null)
                {
                    throw new InvalidUserManagementException("NewRole is required when changing a user's role.");
                }

                if (command.ActorUserId == command.TargetUserId && command.NewRole != Role.Admin)
                {
                    throw new InvalidUserManagementException("An admin cannot demote their own admin role.");
                }

                target.ChangeRole(command.NewRole.Value, utcNow);
                break;

            case ManageUserAction.Suspend:
                if (command.ActorUserId == command.TargetUserId)
                {
                    throw new InvalidUserManagementException("An admin cannot suspend their own account.");
                }

                target.Suspend(utcNow);
                break;

            case ManageUserAction.Reactivate:
                target.Reactivate(utcNow);
                break;

            default:
                throw new InvalidUserManagementException($"Unsupported action '{command.Action}'.");
        }

        await users.UpdateAsync(command.LanguageId, target, cancellationToken);

        return new ManageUserRoleResult(
            target.Id,
            target.LanguageId,
            target.Email,
            target.DisplayName,
            target.Role,
            target.Status);
    }
}
