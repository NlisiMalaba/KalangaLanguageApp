using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface GetPlatformMetricsPort
{
    Task<PlatformMetricsResult> ExecuteAsync(
        GetPlatformMetricsCommand command,
        CancellationToken cancellationToken = default);
}
