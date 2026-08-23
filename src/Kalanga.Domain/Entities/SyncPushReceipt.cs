using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class SyncPushReceipt
{
    internal SyncPushReceipt(
        SyncPushReceiptId id,
        LanguageId languageId,
        UserId userId,
        string clientOperationId,
        DateTimeOffset createdAt)
    {
        Id = id;
        LanguageId = languageId;
        UserId = userId;
        ClientOperationId = clientOperationId;
        CreatedAt = createdAt;
    }

    public SyncPushReceiptId Id { get; }

    public LanguageId LanguageId { get; }

    public UserId UserId { get; }

    public string ClientOperationId { get; }

    public DateTimeOffset CreatedAt { get; }

    public static SyncPushReceipt Create(
        LanguageId languageId,
        UserId userId,
        string clientOperationId,
        DateTimeOffset utcNow)
    {
        return new SyncPushReceipt(
            SyncPushReceiptId.New(),
            languageId,
            userId,
            Guard.Required(clientOperationId, nameof(clientOperationId), DomainRules.MaxSyncClientOperationIdLength),
            utcNow);
    }
}
