using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface ListLanguagesPort
{
    Task<ListLanguagesResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
