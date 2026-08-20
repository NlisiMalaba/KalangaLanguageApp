using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class ContentPackRepository(KalangaDbContext db) : IContentPackRepository
{
    public async Task<ContentPack?> FindByIdAsync(
        LanguageId languageId,
        ContentPackId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.ContentPacks
            .AsNoTracking()
            .FirstOrDefaultAsync(pack => pack.LanguageId == languageId.Value && pack.Id == id.Value, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var lessonIds = await db.ContentPackLessons
            .AsNoTracking()
            .Where(link => link.PackId == id.Value)
            .Select(link => link.LessonId)
            .ToListAsync(cancellationToken);

        return record.ToDomain(lessonIds);
    }

    public async Task<IReadOnlyList<ContentPack>> FindAsync(
        LanguageId languageId,
        Level? level,
        string? category,
        CancellationToken cancellationToken = default)
    {
        var query = db.ContentPacks
            .AsNoTracking()
            .Where(pack => pack.LanguageId == languageId.Value);

        if (level is { } selectedLevel)
        {
            var stored = selectedLevel.ToString();
            query = query.Where(pack => pack.Level == stored);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(pack => pack.Category == category);
        }

        var records = await query
            .OrderBy(pack => pack.Name)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain([]));
    }

    public async Task AddAsync(LanguageId languageId, ContentPack pack, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, pack.LanguageId);
        db.ContentPacks.Add(pack.ToRecord());
        db.ContentPackLessons.AddRange(ToLinks(pack));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LanguageId languageId, ContentPack pack, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, pack.LanguageId);
        var record = await db.ContentPacks.FirstOrDefaultAsync(
            row => row.LanguageId == languageId.Value && row.Id == pack.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"Content pack {pack.Id} was not found for this language.");

        pack.CopyTo(record);

        var existing = await db.ContentPackLessons
            .Where(link => link.PackId == pack.Id.Value)
            .ToListAsync(cancellationToken);
        db.ContentPackLessons.RemoveRange(existing);
        db.ContentPackLessons.AddRange(ToLinks(pack));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<ContentPackLessonRecord> ToLinks(ContentPack pack) =>
        pack.LessonIds.Select(lessonId => new ContentPackLessonRecord
        {
            PackId = pack.Id.Value,
            LessonId = lessonId.Value,
        });
}
