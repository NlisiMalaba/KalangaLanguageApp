using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface GetContentPackManifestPort
{
    Task<GetContentPackManifestResult> ExecuteAsync(
        GetContentPackManifestCommand command,
        CancellationToken cancellationToken = default);
}
