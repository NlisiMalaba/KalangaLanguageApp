using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;

namespace Kalanga.Application.Audio;

internal static class AudioFormatSniffer
{
    public static AudioFileFormat Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length < 3)
        {
            throw new InvalidAudioUploadException("Audio file is too small to determine its format.");
        }

        if (header[0] == (byte)'I' && header[1] == (byte)'D' && header[2] == (byte)'3')
        {
            return AudioFileFormat.Mp3;
        }

        if (header[0] == 0xFF && header.Length >= 2 && (header[1] & 0xE0) == 0xE0 && IsMp3Layer3(header[1]))
        {
            return AudioFileFormat.Mp3;
        }

        if (header[0] == 0xFF && header.Length >= 2 && (header[1] & 0xF6) == 0xF0)
        {
            return AudioFileFormat.Aac;
        }

        if (header.Length >= 12
            && header[4] == (byte)'f'
            && header[5] == (byte)'t'
            && header[6] == (byte)'y'
            && header[7] == (byte)'p')
        {
            return AudioFileFormat.Aac;
        }

        throw new InvalidAudioUploadException("Audio must be MP3 or AAC. The file contents were not recognized.");
    }

    private static bool IsMp3Layer3(byte second) => (second & 0x06) == 0x02;
}
