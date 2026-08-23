namespace Kalanga.Application.Ports.Out;

public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
