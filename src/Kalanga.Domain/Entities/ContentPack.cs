using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class ContentPack
{
    private readonly HashSet<LessonId> _lessonIds;

    internal ContentPack(
        ContentPackId id,
        LanguageId languageId,
        string name,
        Level? level,
        string? category,
        int version,
        long sizeBytes,
        string manifestUrl,
        HashSet<LessonId> lessonIds,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        LanguageId = languageId;
        Name = name;
        Level = level;
        Category = category;
        Version = version;
        SizeBytes = sizeBytes;
        ManifestUrl = manifestUrl;
        _lessonIds = lessonIds;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public ContentPackId Id { get; }

    public LanguageId LanguageId { get; }

    public string Name { get; }

    public Level? Level { get; }

    public string? Category { get; }

    public int Version { get; private set; }

    public long SizeBytes { get; private set; }

    public string ManifestUrl { get; private set; }

    public IReadOnlyCollection<LessonId> LessonIds => _lessonIds;

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ContentPack Create(
        LanguageId languageId,
        string name,
        string manifestUrl,
        long sizeBytes,
        DateTimeOffset utcNow,
        Level? level = null,
        string? category = null,
        IEnumerable<LessonId>? lessonIds = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);

        return new ContentPack(
            ContentPackId.New(),
            languageId,
            Guard.Required(name, nameof(name), 255),
            level,
            string.IsNullOrWhiteSpace(category) ? null : Guard.Required(category, nameof(category), 100),
            version: 1,
            sizeBytes,
            Guard.RequiredText(manifestUrl, nameof(manifestUrl)),
            lessonIds?.ToHashSet() ?? [],
            utcNow,
            utcNow);
    }

    public void AddLesson(LessonId lessonId, DateTimeOffset utcNow)
    {
        _lessonIds.Add(lessonId);
        Touch(utcNow);
    }

    public void ReplaceManifest(string manifestUrl, long sizeBytes, DateTimeOffset utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);
        ManifestUrl = Guard.RequiredText(manifestUrl, nameof(manifestUrl));
        SizeBytes = sizeBytes;
        Version++;
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow) => UpdatedAt = utcNow;
}
