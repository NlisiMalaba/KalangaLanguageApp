using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface RegisterUserPort
{
    Task<RegisterUserResult> ExecuteAsync(RegisterUserCommand command, CancellationToken cancellationToken = default);
}
