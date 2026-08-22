using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class BrowseLessonCatalogUseCase(ILessonRepository lessons) : BrowseLessonCatalogPort
{
    public async Task<BrowseLessonCatalogResult> ExecuteAsync(
        BrowseLessonCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = string.IsNullOrWhiteSpace(command.Category) ? null : command.Category.Trim();
        var published = await lessons.FindPublishedAsync(
            command.LanguageId,
            command.Level,
            category,
            command.Skip,
            command.Take,
            cancellationToken);

        var items = published
            .Select(static lesson => new LessonCatalogItemDto(
                lesson.Id,
                lesson.LanguageId,
                lesson.Title,
                lesson.Level,
                lesson.Category,
                lesson.IsScenario,
                lesson.ScenarioContext,
                lesson.XpReward,
                lesson.UpdatedAt))
            .ToList();

        return new BrowseLessonCatalogResult(items);
    }
}
