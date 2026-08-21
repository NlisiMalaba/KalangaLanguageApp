using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface RefreshTokenPort
{
    Task<AuthenticateUserResult> ExecuteAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default);
}
