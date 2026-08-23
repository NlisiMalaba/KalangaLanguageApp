using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class SyncPushReceiptRepository(KalangaDbContext db) : ISyncPushReceiptRepository
{
    public async Task<bool> ExistsAsync(
        LanguageId languageId,
        UserId userId,
        string clientOperationId,
        CancellationToken cancellationToken = default)
    {
        return await db.SyncPushReceipts
            .AsNoTracking()
            .AnyAsync(
                receipt => receipt.LanguageId == languageId.Value
                           && receipt.UserId == userId.Value
                           && receipt.ClientOperationId == clientOperationId,
                cancellationToken);
    }

    public async Task AddAsync(LanguageId languageId, SyncPushReceipt receipt, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, receipt.LanguageId);
        db.SyncPushReceipts.Add(receipt.ToRecord());
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            db.ChangeTracker.Clear();
            throw new DuplicateSyncPushException(receipt.ClientOperationId);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres
        && postgres.SqlState == PostgresErrorCodes.UniqueViolation;
}
