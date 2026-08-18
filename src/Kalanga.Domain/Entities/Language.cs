using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class Language
{
    private Language(
        LanguageId id,
        string code,
        string name,
        string region,
        bool isActive,
        DateTimeOffset createdAt)
    {
        Id = id;
        Code = code;
        Name = name;
        Region = region;
        IsActive = isActive;
        CreatedAt = createdAt;
    }

    public LanguageId Id { get; }

    public string Code { get; }

    public string Name { get; }

    public string Region { get; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public static Language Create(string code, string name, string region, DateTimeOffset utcNow)
    {
        return new Language(
            LanguageId.New(),
            Guard.Required(code, nameof(code), 10).ToLowerInvariant(),
            Guard.Required(name, nameof(name), 100),
            Guard.Required(region, nameof(region), 100),
            isActive: true,
            utcNow);
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
