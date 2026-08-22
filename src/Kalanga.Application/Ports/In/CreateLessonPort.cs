using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface CreateLessonPort
{
    Task<CreateLessonResult> ExecuteAsync(
        CreateLessonCommand command,
        CancellationToken cancellationToken = default);
}
