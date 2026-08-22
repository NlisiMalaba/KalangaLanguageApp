using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class LanguageVariationRepository(KalangaDbContext db) : ILanguageVariationRepository
{
    public async Task<LanguageVariation?> FindByIdAsync(
        LanguageId languageId,
        LanguageVariationId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.LanguageVariations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                variation => variation.LanguageId == languageId.Value && variation.Id == id.Value,
                cancellationToken);
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<LanguageVariation>> FindByPhraseIdAsync(
        LanguageId languageId,
        PhraseId phraseId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.LanguageVariations
            .AsNoTracking()
            .Where(variation => variation.LanguageId == languageId.Value && variation.PhraseId == phraseId.Value)
            .OrderBy(variation => variation.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task<IReadOnlyList<LanguageVariation>> FindByPhraseIdsAsync(
        LanguageId languageId,
        IReadOnlyCollection<PhraseId> phraseIds,
        CancellationToken cancellationToken = default)
    {
        if (phraseIds.Count == 0)
        {
            return [];
        }

        var ids = phraseIds.Select(static id => id.Value).ToArray();
        var records = await db.LanguageVariations
            .AsNoTracking()
            .Where(variation => variation.LanguageId == languageId.Value && ids.Contains(variation.PhraseId))
            .OrderBy(variation => variation.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task AddAsync(LanguageId languageId, LanguageVariation variation, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, variation.LanguageId);
        db.LanguageVariations.Add(variation.ToRecord());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LanguageId languageId, LanguageVariation variation, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, variation.LanguageId);
        var record = await db.LanguageVariations.FirstOrDefaultAsync(
            row => row.LanguageId == languageId.Value && row.Id == variation.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"Language variation {variation.Id} was not found for this language.");
        variation.CopyTo(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
