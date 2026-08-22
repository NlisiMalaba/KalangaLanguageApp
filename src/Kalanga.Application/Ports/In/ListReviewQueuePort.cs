using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface ListReviewQueuePort
{
    Task<ListReviewQueueResult> ExecuteAsync(
        ListReviewQueueCommand command,
        CancellationToken cancellationToken = default);
}
