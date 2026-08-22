using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface GetAudioPort
{
    Task<GetAudioResult> ExecuteAsync(
        GetAudioCommand command,
        CancellationToken cancellationToken = default);
}
