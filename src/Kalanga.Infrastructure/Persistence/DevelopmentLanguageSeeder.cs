using Kalanga.Domain;
using Kalanga.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence;

internal sealed class DevelopmentLanguageSeeder(KalangaDbContext db)
{
    public async Task EnsureKalangaAsync(CancellationToken cancellationToken = default)
    {
        var exists = await db.Languages
            .AnyAsync(language => language.Id == WellKnownLanguages.KalangaId.Value, cancellationToken);
        if (exists)
        {
            return;
        }

        db.Languages.Add(new LanguageRecord
        {
            Id = WellKnownLanguages.KalangaId.Value,
            Code = WellKnownLanguages.KalangaCode,
            Name = WellKnownLanguages.KalangaName,
            Region = WellKnownLanguages.KalangaRegion,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
