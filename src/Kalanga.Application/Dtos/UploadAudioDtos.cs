using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public sealed record UploadAudioCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    Stream Content,
    long? DeclaredContentLength,
    int DurationMs,
    PhraseId? PhraseId,
    LanguageVariationId? VariationId,
    SpeakerGender SpeakerGender = SpeakerGender.Unspecified,
    string? DialectLabel = null);

public sealed record UploadAudioResult(
    AudioRecordingId AudioRecordingId,
    LanguageId LanguageId,
    PhraseId? PhraseId,
    LanguageVariationId? VariationId,
    string CdnUrl,
    AudioFileFormat FileFormat,
    int FileSizeBytes,
    AudioRecordingStatus Status,
    SpeakerGender SpeakerGender,
    string? DialectLabel,
    int DurationMs);
