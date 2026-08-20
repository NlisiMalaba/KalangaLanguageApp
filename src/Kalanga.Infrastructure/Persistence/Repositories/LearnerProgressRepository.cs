using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class LearnerProgressRepository(KalangaDbContext db) : ILearnerProgressRepository
{
    public async Task<LearnerProgress?> FindByUserAndLessonAsync(
        LanguageId languageId,
        UserId userId,
        LessonId lessonId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.LearnerProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(
                progress => progress.LanguageId == languageId.Value
                            && progress.UserId == userId.Value
                            && progress.LessonId == lessonId.Value,
                cancellationToken);
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<LearnerProgress>> FindByUserAsync(
        LanguageId languageId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.LearnerProgress
            .AsNoTracking()
            .Where(progress => progress.LanguageId == languageId.Value && progress.UserId == userId.Value)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task AddAsync(LanguageId languageId, LearnerProgress progress, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, progress.LanguageId);
        db.LearnerProgress.Add(progress.ToRecord());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LanguageId languageId, LearnerProgress progress, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, progress.LanguageId);
        var record = await db.LearnerProgress.FirstOrDefaultAsync(
            row => row.LanguageId == languageId.Value && row.Id == progress.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"Learner progress {progress.Id} was not found for this language.");
        progress.CopyTo(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
