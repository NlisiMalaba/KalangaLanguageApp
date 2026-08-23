using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface UpvoteRequestPort
{
    Task<UpvoteRequestResult> ExecuteAsync(
        UpvoteRequestCommand command,
        CancellationToken cancellationToken = default);
}
