using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface UpdateStreakPort
{
    Task<UpdateStreakResult> ExecuteAsync(
        UpdateStreakCommand command,
        CancellationToken cancellationToken = default);
}
