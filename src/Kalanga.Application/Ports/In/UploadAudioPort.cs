using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface UploadAudioPort
{
    Task<UploadAudioResult> ExecuteAsync(
        UploadAudioCommand command,
        CancellationToken cancellationToken = default);
}
