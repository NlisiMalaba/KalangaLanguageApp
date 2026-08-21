using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface AuthenticateUserPort
{
    Task<AuthenticateUserResult> ExecuteAsync(
        AuthenticateUserCommand command,
        CancellationToken cancellationToken = default);
}
