using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class SyncCheckpoint
{
    internal SyncCheckpoint(
        SyncCheckpointId id,
        LanguageId languageId,
        UserId userId,
        DateTimeOffset lastSyncedAt,
        long syncVersion)
    {
        Id = id;
        LanguageId = languageId;
        UserId = userId;
        LastSyncedAt = lastSyncedAt;
        SyncVersion = syncVersion;
    }

    public SyncCheckpointId Id { get; }

    public LanguageId LanguageId { get; }

    public UserId UserId { get; }

    public DateTimeOffset LastSyncedAt { get; private set; }

    public long SyncVersion { get; private set; }

    public static SyncCheckpoint Create(LanguageId languageId, UserId userId, DateTimeOffset utcNow)
    {
        return new SyncCheckpoint(
            SyncCheckpointId.New(),
            languageId,
            userId,
            utcNow,
            syncVersion: 0);
    }

    public void Advance(long syncVersion, DateTimeOffset lastSyncedAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(syncVersion);
        LastSyncedAt = lastSyncedAt;
        SyncVersion = syncVersion;
    }
}
