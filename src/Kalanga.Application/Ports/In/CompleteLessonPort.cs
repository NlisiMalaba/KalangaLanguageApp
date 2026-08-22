using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface CompleteLessonPort
{
    Task<CompleteLessonResult> ExecuteAsync(
        CompleteLessonCommand command,
        CancellationToken cancellationToken = default);
}
