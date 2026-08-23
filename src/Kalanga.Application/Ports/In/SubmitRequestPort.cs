using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface SubmitRequestPort
{
    Task<SubmitRequestResult> ExecuteAsync(
        SubmitRequestCommand command,
        CancellationToken cancellationToken = default);
}
