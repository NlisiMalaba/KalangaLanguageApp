using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface IAudioStorage
{
    Task<string> PutAsync(
        LanguageId languageId,
        PhraseId phraseId,
        AudioRecordingId recordingId,
        AudioFileFormat format,
        Stream content,
        int contentLength,
        CancellationToken cancellationToken = default);

    Task TryDeleteAsync(
        LanguageId languageId,
        PhraseId phraseId,
        AudioRecordingId recordingId,
        AudioFileFormat format,
        CancellationToken cancellationToken = default);
}
