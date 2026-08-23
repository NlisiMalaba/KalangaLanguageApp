using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface ILanguageRepository
{
    Task<Language?> FindByIdAsync(LanguageId id, CancellationToken cancellationToken = default);

    Task<Language?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Language>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Language language, CancellationToken cancellationToken = default);
}
