using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface ISyncCheckpointRepository
{
    Task<SyncCheckpoint?> FindByUserAsync(
        LanguageId languageId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, SyncCheckpoint checkpoint, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, SyncCheckpoint checkpoint, CancellationToken cancellationToken = default);
}
