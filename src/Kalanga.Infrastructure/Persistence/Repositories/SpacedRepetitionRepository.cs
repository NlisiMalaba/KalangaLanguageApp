using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class SpacedRepetitionRepository(KalangaDbContext db) : ISpacedRepetitionRepository
{
    public async Task<SpacedRepetitionRecord?> FindByUserPhraseAndVariationAsync(
        LanguageId languageId,
        UserId userId,
        PhraseId phraseId,
        LanguageVariationId? variationId,
        CancellationToken cancellationToken = default)
    {
        var variation = variationId?.Value;
        var record = await db.SpacedRepetitionRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                srs => srs.LanguageId == languageId.Value
                       && srs.UserId == userId.Value
                       && srs.PhraseId == phraseId.Value
                       && srs.VariationId == variation,
                cancellationToken);
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<SpacedRepetitionRecord>> FindDueAsync(
        LanguageId languageId,
        UserId userId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        var records = await db.SpacedRepetitionRecords
            .AsNoTracking()
            .Where(srs => srs.LanguageId == languageId.Value
                          && srs.UserId == userId.Value
                          && srs.NextReviewAt <= asOfDate)
            .OrderBy(srs => srs.NextReviewAt)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task<IReadOnlyList<SpacedRepetitionRecord>> FindByUserAsync(
        LanguageId languageId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.SpacedRepetitionRecords
            .AsNoTracking()
            .Where(srs => srs.LanguageId == languageId.Value && srs.UserId == userId.Value)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task<IReadOnlyList<SpacedRepetitionRecord>> FindUpdatedSinceAsync(
        LanguageId languageId,
        UserId userId,
        DateTimeOffset updatedAfter,
        CancellationToken cancellationToken = default)
    {
        var records = await db.SpacedRepetitionRecords
            .AsNoTracking()
            .Where(srs => srs.LanguageId == languageId.Value
                          && srs.UserId == userId.Value
                          && srs.UpdatedAt > updatedAfter)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task AddAsync(LanguageId languageId, SpacedRepetitionRecord record, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, record.LanguageId);
        db.SpacedRepetitionRecords.Add(record.ToRecord());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LanguageId languageId, SpacedRepetitionRecord record, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, record.LanguageId);
        var row = await db.SpacedRepetitionRecords.FirstOrDefaultAsync(
            srs => srs.LanguageId == languageId.Value && srs.Id == record.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"SRS record {record.Id} was not found for this language.");
        record.CopyTo(row);
        await db.SaveChangesAsync(cancellationToken);
    }
}
