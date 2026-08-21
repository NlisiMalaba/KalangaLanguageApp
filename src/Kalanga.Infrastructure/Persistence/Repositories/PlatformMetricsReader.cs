using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class PlatformMetricsReader(KalangaDbContext db) : IPlatformMetricsReader
{
    public async Task<PlatformMetricsSnapshot> GetAsync(
        LanguageId languageId,
        CancellationToken cancellationToken = default)
    {
        var totalUsers = await db.Users
            .AsNoTracking()
            .CountAsync(user => user.LanguageId == languageId.Value, cancellationToken);

        var published = LessonStatus.Published.ToString();
        var totalPublishedLessons = await db.Lessons
            .AsNoTracking()
            .CountAsync(
                lesson => lesson.LanguageId == languageId.Value && lesson.Status == published,
                cancellationToken);

        var approved = AudioRecordingStatus.Approved.ToString();
        var totalApprovedAudio = await db.AudioRecordings
            .AsNoTracking()
            .CountAsync(
                audio => audio.LanguageId == languageId.Value && audio.Status == approved,
                cancellationToken);

        return new PlatformMetricsSnapshot(totalUsers, totalPublishedLessons, totalApprovedAudio);
    }
}
