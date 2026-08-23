using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface FulfillRequestPort
{
    Task<FulfillRequestResult> ExecuteAsync(
        FulfillRequestCommand command,
        CancellationToken cancellationToken = default);
}
