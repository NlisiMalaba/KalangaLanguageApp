using Kalanga.Application.Dtos;

namespace Kalanga.Application.Ports.In;

public interface BrowseLessonCatalogPort
{
    Task<BrowseLessonCatalogResult> ExecuteAsync(
        BrowseLessonCatalogCommand command,
        CancellationToken cancellationToken = default);
}
