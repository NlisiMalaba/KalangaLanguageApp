using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface LogoutPort
{
    Task ExecuteAsync(LogoutCommand command, CancellationToken cancellationToken = default);
}
