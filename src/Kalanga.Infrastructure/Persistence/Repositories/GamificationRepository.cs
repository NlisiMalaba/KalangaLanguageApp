using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class GamificationRepository(KalangaDbContext db) : IGamificationRepository
{
    public async Task<LearnerGamification?> FindByUserAsync(
        LanguageId languageId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.LearnerGamification
            .AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.LanguageId == languageId.Value && row.UserId == userId.Value,
                cancellationToken);
        return record?.ToDomain();
    }

    public async Task AddAsync(LanguageId languageId, LearnerGamification gamification, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, gamification.LanguageId);
        db.LearnerGamification.Add(gamification.ToRecord());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LanguageId languageId, LearnerGamification gamification, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, gamification.LanguageId);
        var record = await db.LearnerGamification.FirstOrDefaultAsync(
            row => row.LanguageId == languageId.Value && row.Id == gamification.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"Gamification {gamification.Id} was not found for this language.");
        gamification.CopyTo(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
