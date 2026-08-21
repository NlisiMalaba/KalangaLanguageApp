using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface ManageUserRolePort
{
    Task<ManageUserRoleResult> ExecuteAsync(
        ManageUserRoleCommand command,
        CancellationToken cancellationToken = default);
}
