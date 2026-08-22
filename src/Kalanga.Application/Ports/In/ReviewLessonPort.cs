using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface ReviewLessonPort
{
    Task<ReviewLessonResult> ExecuteAsync(
        ReviewLessonCommand command,
        CancellationToken cancellationToken = default);
}
