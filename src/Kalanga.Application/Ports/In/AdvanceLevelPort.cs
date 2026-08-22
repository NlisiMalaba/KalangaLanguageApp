using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface AdvanceLevelPort
{
    Task<AdvanceLevelResult> ExecuteAsync(
        AdvanceLevelCommand command,
        CancellationToken cancellationToken = default);
}
