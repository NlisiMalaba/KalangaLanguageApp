using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;

namespace Kalanga.Application.UseCases;

internal sealed class CachingBrowseLessonCatalogUseCase(
    BrowseLessonCatalogUseCase inner,
    ICatalogCache cache) : BrowseLessonCatalogPort
{
    public async Task<BrowseLessonCatalogResult> ExecuteAsync(
        BrowseLessonCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = string.IsNullOrWhiteSpace(command.Category) ? null : command.Category.Trim();
        var cached = await cache.TryGetAsync(
            command.LanguageId,
            command.Level,
            category,
            command.Skip,
            command.Take,
            cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var result = await inner.ExecuteAsync(command, cancellationToken);
        await cache.SetAsync(
            command.LanguageId,
            command.Level,
            category,
            command.Skip,
            command.Take,
            result,
            cancellationToken);
        return result;
    }
}
