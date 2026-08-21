using Kalanga.Application.Ports.Out;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(KalangaDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(
        LanguageId languageId,
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        db.RefreshTokens.Add(new RefreshTokenRecord
        {
            Id = Guid.CreateVersion7(),
            LanguageId = languageId.Value,
            UserId = userId.Value,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            RevokedAt = null,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredRefreshToken?> FindActiveByHashAsync(
        LanguageId languageId,
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        var utcNow = DateTimeOffset.UtcNow;

        var record = await db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                token => token.LanguageId == languageId.Value
                    && token.TokenHash == tokenHash
                    && token.RevokedAt == null
                    && token.ExpiresAt > utcNow,
                cancellationToken);

        return record is null
            ? null
            : new StoredRefreshToken(
                record.Id,
                LanguageId.From(record.LanguageId),
                UserId.From(record.UserId),
                record.TokenHash,
                record.ExpiresAt,
                record.CreatedAt,
                record.RevokedAt);
    }

    public async Task RevokeAsync(
        LanguageId languageId,
        Guid tokenId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default)
    {
        var record = await db.RefreshTokens.FirstOrDefaultAsync(
            token => token.LanguageId == languageId.Value && token.Id == tokenId,
            cancellationToken)
            ?? throw new InvalidOperationException($"Refresh token {tokenId} was not found for this language.");

        if (record.RevokedAt is null)
        {
            record.RevokedAt = revokedAt;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
