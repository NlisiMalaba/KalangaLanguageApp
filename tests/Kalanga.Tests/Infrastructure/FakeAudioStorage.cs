using Kalanga.Application.Audio;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Tests.Infrastructure;

public sealed class FakeAudioStorage : IAudioStorage
{
    public List<string> Puts { get; } = [];

    public Task<string> PutAsync(
        LanguageId languageId,
        PhraseId phraseId,
        AudioRecordingId recordingId,
        AudioFileFormat format,
        Stream content,
        int contentLength,
        CancellationToken cancellationToken = default)
    {
        var key = AudioObjectKeys.For(languageId, phraseId, recordingId, format);
        Puts.Add(key);
        return Task.FromResult($"https://cdn.test/{key}");
    }

    public Task TryDeleteAsync(
        LanguageId languageId,
        PhraseId phraseId,
        AudioRecordingId recordingId,
        AudioFileFormat format,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
