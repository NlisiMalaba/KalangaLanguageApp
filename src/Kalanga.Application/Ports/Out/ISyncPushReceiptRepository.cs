using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface ISyncPushReceiptRepository
{
    Task<bool> ExistsAsync(
        LanguageId languageId,
        UserId userId,
        string clientOperationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, SyncPushReceipt receipt, CancellationToken cancellationToken = default);
}
