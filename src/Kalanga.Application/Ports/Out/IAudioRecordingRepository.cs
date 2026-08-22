using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface IAudioRecordingRepository
{
    Task<AudioRecording?> FindByIdAsync(
        LanguageId languageId,
        AudioRecordingId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AudioRecording>> FindByPhraseIdAsync(
        LanguageId languageId,
        PhraseId phraseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AudioRecording>> FindByPhraseIdsAsync(
        LanguageId languageId,
        IReadOnlyCollection<PhraseId> phraseIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AudioRecording>> FindByVariationIdsAsync(
        LanguageId languageId,
        IReadOnlyCollection<LanguageVariationId> variationIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AudioRecording>> FindPendingReviewAsync(
        LanguageId languageId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, AudioRecording recording, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, AudioRecording recording, CancellationToken cancellationToken = default);
}
