using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface GetProgressPort
{
    Task<GetProgressResult> ExecuteAsync(
        GetProgressCommand command,
        CancellationToken cancellationToken = default);
}
