using Kalanga.Application.Audio;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;

namespace Kalanga.Tests.Application;

public sealed class AudioFormatSnifferTests
{
    [Fact]
    public void Id3_header_is_detected_as_mp3()
    {
        var header = "ID3"u8.ToArray().Concat(new byte[13]).ToArray();
        Assert.Equal(AudioFileFormat.Mp3, AudioFormatSniffer.Detect(header));
    }

    [Fact]
    public void Mpeg_layer_iii_frame_is_detected_as_mp3()
    {
        Assert.Equal(AudioFileFormat.Mp3, AudioFormatSniffer.Detect([0xFF, 0xFB, 0x90, 0x00]));
    }

    [Fact]
    public void Adts_header_is_detected_as_aac()
    {
        Assert.Equal(AudioFileFormat.Aac, AudioFormatSniffer.Detect([0xFF, 0xF1, 0x50, 0x80]));
    }

    [Fact]
    public void Ftyp_box_is_detected_as_aac()
    {
        var header = new byte[]
        {
            0x00, 0x00, 0x00, 0x20,
            (byte)'f', (byte)'t', (byte)'y', (byte)'p',
            (byte)'M', (byte)'4', (byte)'A', (byte)' ',
        };
        Assert.Equal(AudioFileFormat.Aac, AudioFormatSniffer.Detect(header));
    }

    [Fact]
    public void Unrecognized_bytes_are_rejected()
    {
        Assert.Throws<InvalidAudioUploadException>(() =>
            AudioFormatSniffer.Detect("GIF89a"u8.ToArray()));
    }
}
