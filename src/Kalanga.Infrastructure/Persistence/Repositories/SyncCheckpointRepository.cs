using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class SyncCheckpointRepository(KalangaDbContext db) : ISyncCheckpointRepository
{
    public async Task<SyncCheckpoint?> FindByUserAsync(
        LanguageId languageId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.SyncCheckpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(
                checkpoint => checkpoint.LanguageId == languageId.Value && checkpoint.UserId == userId.Value,
                cancellationToken);
        return record?.ToDomain();
    }

    public async Task AddAsync(LanguageId languageId, SyncCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, checkpoint.LanguageId);
        db.SyncCheckpoints.Add(checkpoint.ToRecord());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LanguageId languageId, SyncCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, checkpoint.LanguageId);
        var record = await db.SyncCheckpoints.FirstOrDefaultAsync(
            row => row.LanguageId == languageId.Value && row.Id == checkpoint.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"Sync checkpoint {checkpoint.Id} was not found for this language.");
        checkpoint.CopyTo(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
