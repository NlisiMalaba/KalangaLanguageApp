using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface GetLessonDraftPort
{
    Task<GetLessonDraftResult> ExecuteAsync(
        GetLessonDraftCommand command,
        CancellationToken cancellationToken = default);
}
