using Kalanga.Application.Ports.Out;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(KalangaDbContext db) : IUnitOfWork
{
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (db.Database.CurrentTransaction is not null)
        {
            return await action(cancellationToken);
        }

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();
                throw;
            }
        });
    }
}
