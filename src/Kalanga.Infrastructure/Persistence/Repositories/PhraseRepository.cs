using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class PhraseRepository(KalangaDbContext db) : IPhraseRepository
{
    public async Task<Phrase?> FindByIdAsync(LanguageId languageId, PhraseId id, CancellationToken cancellationToken = default)
    {
        var record = await db.Phrases
            .AsNoTracking()
            .FirstOrDefaultAsync(phrase => phrase.LanguageId == languageId.Value && phrase.Id == id.Value, cancellationToken);
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Phrase>> FindByLessonIdAsync(
        LanguageId languageId,
        LessonId lessonId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.Phrases
            .AsNoTracking()
            .Where(phrase => phrase.LanguageId == languageId.Value && phrase.LessonId == lessonId.Value)
            .OrderBy(phrase => phrase.SortOrder)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task<IReadOnlyList<Phrase>> FindByLessonIdsAsync(
        LanguageId languageId,
        IReadOnlyCollection<LessonId> lessonIds,
        CancellationToken cancellationToken = default)
    {
        if (lessonIds.Count == 0)
        {
            return [];
        }

        var values = lessonIds.Select(static id => id.Value).Distinct().ToArray();
        var records = await db.Phrases
            .AsNoTracking()
            .Where(phrase => phrase.LanguageId == languageId.Value && values.Contains(phrase.LessonId))
            .OrderBy(phrase => phrase.SortOrder)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task<IReadOnlyList<Phrase>> FindByIdsAsync(
        LanguageId languageId,
        IReadOnlyCollection<PhraseId> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var values = ids.Select(static id => id.Value).Distinct().ToArray();
        var records = await db.Phrases
            .AsNoTracking()
            .Where(phrase => phrase.LanguageId == languageId.Value && values.Contains(phrase.Id))
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task AddAsync(LanguageId languageId, Phrase phrase, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, phrase.LanguageId);
        db.Phrases.Add(phrase.ToRecord());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LanguageId languageId, Phrase phrase, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, phrase.LanguageId);
        var record = await db.Phrases.FirstOrDefaultAsync(
            row => row.LanguageId == languageId.Value && row.Id == phrase.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"Phrase {phrase.Id} was not found for this language.");
        phrase.CopyTo(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
