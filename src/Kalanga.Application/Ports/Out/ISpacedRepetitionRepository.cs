using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface ISpacedRepetitionRepository
{
    Task<SpacedRepetitionRecord?> FindByUserPhraseAndVariationAsync(
        LanguageId languageId,
        UserId userId,
        PhraseId phraseId,
        LanguageVariationId? variationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpacedRepetitionRecord>> FindDueAsync(
        LanguageId languageId,
        UserId userId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpacedRepetitionRecord>> FindByUserAsync(
        LanguageId languageId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpacedRepetitionRecord>> FindUpdatedSinceAsync(
        LanguageId languageId,
        UserId userId,
        DateTimeOffset updatedAfter,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, SpacedRepetitionRecord record, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, SpacedRepetitionRecord record, CancellationToken cancellationToken = default);
}
