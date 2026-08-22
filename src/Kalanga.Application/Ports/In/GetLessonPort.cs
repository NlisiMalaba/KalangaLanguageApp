using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface GetLessonPort
{
    Task<GetLessonResult> ExecuteAsync(
        GetLessonCommand command,
        CancellationToken cancellationToken = default);
}
