using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Audio;

public static class AudioObjectKeys
{
    public static string For(
        LanguageId languageId,
        PhraseId phraseId,
        AudioRecordingId recordingId,
        AudioFileFormat format)
    {
        var extension = format == AudioFileFormat.Mp3 ? "mp3" : "aac";
        return $"audio/{languageId.Value}/{phraseId.Value}/{recordingId.Value}.{extension}";
    }

    public static string ContentType(AudioFileFormat format) =>
        format == AudioFileFormat.Mp3 ? "audio/mpeg" : "audio/aac";
}
