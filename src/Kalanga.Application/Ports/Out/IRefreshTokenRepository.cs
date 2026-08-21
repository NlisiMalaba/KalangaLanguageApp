using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface IRefreshTokenRepository
{
    Task AddAsync(
        LanguageId languageId,
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<StoredRefreshToken?> FindActiveByHashAsync(
        LanguageId languageId,
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        LanguageId languageId,
        Guid tokenId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default);
}

public sealed record StoredRefreshToken(
    Guid Id,
    LanguageId LanguageId,
    UserId UserId,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RevokedAt);
