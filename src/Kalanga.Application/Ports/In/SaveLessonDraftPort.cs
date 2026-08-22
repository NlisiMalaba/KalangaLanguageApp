using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface SaveLessonDraftPort
{
    Task<SaveLessonDraftResult> ExecuteAsync(
        SaveLessonDraftCommand command,
        CancellationToken cancellationToken = default);
}
