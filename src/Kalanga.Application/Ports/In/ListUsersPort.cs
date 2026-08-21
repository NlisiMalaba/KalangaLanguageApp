using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface ListUsersPort
{
    Task<ListUsersResult> ExecuteAsync(ListUsersCommand command, CancellationToken cancellationToken = default);
}
