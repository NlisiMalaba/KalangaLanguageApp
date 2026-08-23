using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface PullSyncPort
{
    Task<PullSyncResult> ExecuteAsync(
        PullSyncCommand command,
        CancellationToken cancellationToken = default);
}
