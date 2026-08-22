using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface ListContentPacksPort
{
    Task<ListContentPacksResult> ExecuteAsync(
        ListContentPacksCommand command,
        CancellationToken cancellationToken = default);
}
