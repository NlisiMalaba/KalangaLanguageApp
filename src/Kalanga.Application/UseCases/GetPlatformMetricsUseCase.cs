using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = nameof(Role.Admin))]
public sealed class GetPlatformMetricsUseCase(
    IUserRepository users,
    IPlatformMetricsReader metrics) : GetPlatformMetricsPort
{
    public async Task<PlatformMetricsResult> ExecuteAsync(
        GetPlatformMetricsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.Role != Role.Admin)
        {
            throw new UnauthorizedRoleException("view platform metrics", Role.Admin);
        }

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var snapshot = await metrics.GetAsync(command.LanguageId, cancellationToken);
        return new PlatformMetricsResult(
            snapshot.TotalUsers,
            snapshot.TotalPublishedLessons,
            snapshot.TotalApprovedAudioRecordings);
    }
}
