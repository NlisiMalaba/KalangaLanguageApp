using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface SyncProgressPort
{
    Task<SyncProgressResult> ExecuteAsync(
        SyncProgressCommand command,
        CancellationToken cancellationToken = default);
}
