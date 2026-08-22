using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface OverrideLessonPublicationPort
{
    Task<OverrideLessonPublicationResult> ExecuteAsync(
        OverrideLessonPublicationCommand command,
        CancellationToken cancellationToken = default);
}
