using Kalanga.Application.Dtos;
using Kalanga.Domain.Entities;

namespace Kalanga.Application;

internal static class LanguageMapping
{
    public static LanguageDto ToDto(this Language language) =>
        new(
            language.Id,
            language.Code,
            language.Name,
            language.Region,
            language.IsActive);
}
