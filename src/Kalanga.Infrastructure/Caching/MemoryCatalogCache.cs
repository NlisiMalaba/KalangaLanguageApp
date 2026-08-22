using System.Collections.Concurrent;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Infrastructure.Caching;

internal sealed class MemoryCatalogCache : ICatalogCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();

    public Task<BrowseLessonCatalogResult?> TryGetAsync(
        LanguageId languageId,
        Level? level,
        string? category,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_entries.TryGetValue(Key(languageId, level, category, skip, take), out var entry)
            && entry.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return Task.FromResult<BrowseLessonCatalogResult?>(entry.Result);
        }

        return Task.FromResult<BrowseLessonCatalogResult?>(null);
    }

    public Task SetAsync(
        LanguageId languageId,
        Level? level,
        string? category,
        int skip,
        int take,
        BrowseLessonCatalogResult result,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entries[Key(languageId, level, category, skip, take)] =
            new CacheEntry(result, DateTimeOffset.UtcNow.Add(Ttl));
        return Task.CompletedTask;
    }

    public void Invalidate(LanguageId languageId)
    {
        var prefix = Prefix(languageId);
        foreach (var key in _entries.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                _entries.TryRemove(key, out _);
            }
        }
    }

    private static string Prefix(LanguageId languageId) => $"catalog:{languageId.Value:N}:";

    private static string Key(LanguageId languageId, Level? level, string? category, int skip, int take) =>
        $"{Prefix(languageId)}{level?.ToString() ?? "_"}:{category ?? "_"}:{skip}:{take}";

    private sealed record CacheEntry(BrowseLessonCatalogResult Result, DateTimeOffset ExpiresAt);
}
