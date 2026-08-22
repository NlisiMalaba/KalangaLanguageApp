using Kalanga.Domain.Enums;

namespace Kalanga.Api.Contracts.Audio;

public sealed class UploadAudioRequest
{
    public required IFormFile File { get; init; }

    public int DurationMs { get; init; }

    public Guid? PhraseId { get; init; }

    public Guid? VariationId { get; init; }

    public SpeakerGender SpeakerGender { get; init; } = SpeakerGender.Unspecified;

    public string? DialectLabel { get; init; }
}
