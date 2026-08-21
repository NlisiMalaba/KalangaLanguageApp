using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[AllowAnonymous]
public sealed class RefreshTokenUseCase(
    IUserRepository users,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokens) : RefreshTokenPort
{
    public async Task<AuthenticateUserResult> ExecuteAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RefreshToken);

        var tokenHash = tokenService.HashRefreshToken(command.RefreshToken);
        var stored = await refreshTokens.FindActiveByHashAsync(
            command.LanguageId,
            tokenHash,
            cancellationToken);

        if (stored is null)
        {
            throw new InvalidRefreshTokenException();
        }

        var user = await users.FindByIdAsync(command.LanguageId, stored.UserId, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        if (user.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var utcNow = DateTimeOffset.UtcNow;
        await refreshTokens.RevokeAsync(command.LanguageId, stored.Id, utcNow, cancellationToken);

        var accessToken = tokenService.CreateAccessToken(user);
        var newRefresh = tokenService.CreateRefreshToken();
        await refreshTokens.AddAsync(
            user.LanguageId,
            user.Id,
            newRefresh.TokenHash,
            newRefresh.ExpiresAt,
            utcNow,
            cancellationToken);

        return new AuthenticateUserResult(
            user.Id,
            user.LanguageId,
            user.Email,
            user.DisplayName,
            user.Role,
            accessToken.Token,
            newRefresh.PlainTextToken,
            accessToken.ExpiresAt,
            newRefresh.ExpiresAt);
    }
}
