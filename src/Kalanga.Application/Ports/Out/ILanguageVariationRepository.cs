using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface ILanguageVariationRepository
{
    Task<LanguageVariation?> FindByIdAsync(
        LanguageId languageId,
        LanguageVariationId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LanguageVariation>> FindByPhraseIdAsync(
        LanguageId languageId,
        PhraseId phraseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LanguageVariation>> FindByPhraseIdsAsync(
        LanguageId languageId,
        IReadOnlyCollection<PhraseId> phraseIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, LanguageVariation variation, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, LanguageVariation variation, CancellationToken cancellationToken = default);
}
