using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface IPlatformMetricsReader
{
    Task<PlatformMetricsSnapshot> GetAsync(LanguageId languageId, CancellationToken cancellationToken = default);
}

public sealed record PlatformMetricsSnapshot(
    int TotalUsers,
    int TotalPublishedLessons,
    int TotalApprovedAudioRecordings);
