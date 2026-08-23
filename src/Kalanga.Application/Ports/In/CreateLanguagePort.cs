using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface CreateLanguagePort
{
    Task<CreateLanguageResult> ExecuteAsync(
        CreateLanguageCommand command,
        CancellationToken cancellationToken = default);
}
