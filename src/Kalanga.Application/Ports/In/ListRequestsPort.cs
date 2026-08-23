using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface ListRequestsPort
{
    Task<ListRequestsResult> ExecuteAsync(
        ListRequestsCommand command,
        CancellationToken cancellationToken = default);
}
