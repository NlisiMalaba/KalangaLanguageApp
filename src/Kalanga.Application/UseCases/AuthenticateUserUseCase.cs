using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Exceptions;

namespace Kalanga.Application.UseCases;

public sealed class AuthenticateUserUseCase(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokens) : AuthenticateUserPort
{
    public async Task<AuthenticateUserResult> ExecuteAsync(
        AuthenticateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await users.FindByEmailAsync(command.LanguageId, command.Email, cancellationToken);
        var passwordHash = user?.PasswordHash ?? passwordHasher.TimingPadHash;
        var credentialsValid = passwordHasher.Verify(command.Password, passwordHash);

        if (user is null || !credentialsValid)
        {
            throw new InvalidCredentialsException();
        }

        if (user.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var accessToken = tokenService.CreateAccessToken(user);
        var refreshToken = tokenService.CreateRefreshToken();
        var utcNow = DateTimeOffset.UtcNow;

        await refreshTokens.AddAsync(
            user.LanguageId,
            user.Id,
            refreshToken.TokenHash,
            refreshToken.ExpiresAt,
            utcNow,
            cancellationToken);

        return new AuthenticateUserResult(
            user.Id,
            user.LanguageId,
            user.Email,
            user.DisplayName,
            user.Role,
            accessToken.Token,
            refreshToken.PlainTextToken,
            accessToken.ExpiresAt,
            refreshToken.ExpiresAt);
    }
}
