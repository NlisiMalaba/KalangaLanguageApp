using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface SubmitLessonForReviewPort
{
    Task<SubmitLessonForReviewResult> ExecuteAsync(
        SubmitLessonForReviewCommand command,
        CancellationToken cancellationToken = default);
}
