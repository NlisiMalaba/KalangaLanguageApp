namespace Kalanga.Infrastructure.Security;

public sealed class ContentPacksOptions
{
    public const string SectionName = "ContentPacks";

    public required string SigningKey { get; init; }
}
