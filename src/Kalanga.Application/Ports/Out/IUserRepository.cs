using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(LanguageId languageId, UserId id, CancellationToken cancellationToken = default);

    Task<User?> FindByEmailAsync(LanguageId languageId, string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListAsync(
        LanguageId languageId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, User user, CancellationToken cancellationToken = default);
}
