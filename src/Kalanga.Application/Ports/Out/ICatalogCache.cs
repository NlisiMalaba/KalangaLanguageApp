using Kalanga.Application.Dtos;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface ICatalogCache
{
    Task<BrowseLessonCatalogResult?> TryGetAsync(
        LanguageId languageId,
        Level? level,
        string? category,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        LanguageId languageId,
        Level? level,
        string? category,
        int skip,
        int take,
        BrowseLessonCatalogResult result,
        CancellationToken cancellationToken = default);

    void Invalidate(LanguageId languageId);
}
