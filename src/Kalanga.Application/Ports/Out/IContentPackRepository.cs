using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface IContentPackRepository
{
    Task<ContentPack?> FindByIdAsync(
        LanguageId languageId,
        ContentPackId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContentPack>> FindAsync(
        LanguageId languageId,
        Level? level,
        string? category,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, ContentPack pack, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, ContentPack pack, CancellationToken cancellationToken = default);
}
