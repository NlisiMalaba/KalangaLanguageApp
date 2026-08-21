using Kalanga.Domain.Entities;

namespace Kalanga.Application.Ports.Out;

public interface ITokenService
{
    IssuedAccessToken CreateAccessToken(User user);

    IssuedRefreshToken CreateRefreshToken();

    string HashRefreshToken(string plainTextToken);
}

public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);

public sealed record IssuedRefreshToken(
    string PlainTextToken,
    string TokenHash,
    DateTimeOffset ExpiresAt);
