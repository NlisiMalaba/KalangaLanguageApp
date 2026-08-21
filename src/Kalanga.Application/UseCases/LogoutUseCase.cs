using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[AllowAnonymous]
public sealed class LogoutUseCase(
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokens) : LogoutPort
{
    public async Task ExecuteAsync(LogoutCommand command, CancellationToken cancellationToken = default)
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
            return;
        }

        await refreshTokens.RevokeAsync(
            command.LanguageId,
            stored.Id,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }
}
