using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain;

public static class WellKnownLanguages
{
    public static LanguageId KalangaId { get; } =
        LanguageId.From(Guid.Parse("a1b2c3d4-e5f6-4780-8bcd-ef1234567890"));

    public const string KalangaCode = "kck";

    public const string KalangaName = "Kalanga";

    public const string KalangaRegion = "Zimbabwe / Botswana";
}
