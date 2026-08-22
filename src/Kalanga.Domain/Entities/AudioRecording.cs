using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class AudioRecording
{
    internal AudioRecording(
        AudioRecordingId id,
        LanguageId languageId,
        PhraseId? phraseId,
        LanguageVariationId? variationId,
        UserId contributorId,
        string cdnUrl,
        AudioFileFormat fileFormat,
        int fileSizeBytes,
        SpeakerGender speakerGender,
        string? dialectLabel,
        AudioRecordingStatus status,
        int durationMs,
        DateTimeOffset createdAt)
    {
        Id = id;
        LanguageId = languageId;
        PhraseId = phraseId;
        VariationId = variationId;
        ContributorId = contributorId;
        CdnUrl = cdnUrl;
        FileFormat = fileFormat;
        FileSizeBytes = fileSizeBytes;
        SpeakerGender = speakerGender;
        DialectLabel = dialectLabel;
        Status = status;
        DurationMs = durationMs;
        CreatedAt = createdAt;
    }

    public AudioRecordingId Id { get; }

    public LanguageId LanguageId { get; }

    public PhraseId? PhraseId { get; }

    public LanguageVariationId? VariationId { get; }

    public UserId ContributorId { get; }

    public string CdnUrl { get; }

    public AudioFileFormat FileFormat { get; }

    public int FileSizeBytes { get; }

    public SpeakerGender SpeakerGender { get; }

    public string? DialectLabel { get; }

    public AudioRecordingStatus Status { get; private set; }

    public int DurationMs { get; }

    public DateTimeOffset CreatedAt { get; }

    public bool IsPlayableByLearners => Status == AudioRecordingStatus.Approved;

    public static AudioRecording Create(
        LanguageId languageId,
        UserId contributorId,
        string cdnUrl,
        AudioFileFormat fileFormat,
        int fileSizeBytes,
        int durationMs,
        DateTimeOffset utcNow,
        PhraseId? phraseId = null,
        LanguageVariationId? variationId = null,
        SpeakerGender speakerGender = SpeakerGender.Unspecified,
        string? dialectLabel = null,
        AudioRecordingId? id = null)
    {
        if (phraseId is null && variationId is null)
        {
            throw new InvalidAudioRecordingException("Audio must belong to a phrase or a language variation.");
        }

        if (fileSizeBytes <= 0 || fileSizeBytes > DomainRules.MaxAudioFileSizeBytes)
        {
            throw new InvalidAudioRecordingException(
                $"Audio must be between 1 and {DomainRules.MaxAudioFileSizeBytes} bytes.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationMs);

        return new AudioRecording(
            id ?? AudioRecordingId.New(),
            languageId,
            phraseId,
            variationId,
            contributorId,
            Guard.RequiredText(cdnUrl, nameof(cdnUrl)),
            fileFormat,
            fileSizeBytes,
            speakerGender,
            string.IsNullOrWhiteSpace(dialectLabel) ? null : Guard.Required(dialectLabel, nameof(dialectLabel), 100),
            AudioRecordingStatus.PendingReview,
            durationMs,
            utcNow);
    }

    public void Approve() => Status = AudioRecordingStatus.Approved;

    public void Reject() => Status = AudioRecordingStatus.Rejected;
}
