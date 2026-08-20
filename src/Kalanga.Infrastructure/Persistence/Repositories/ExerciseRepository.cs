using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class ExerciseRepository(KalangaDbContext db) : IExerciseRepository
{
    public async Task<Exercise?> FindByIdAsync(LanguageId languageId, ExerciseId id, CancellationToken cancellationToken = default)
    {
        var record = await db.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(exercise => exercise.LanguageId == languageId.Value && exercise.Id == id.Value, cancellationToken);
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Exercise>> FindByLessonIdAsync(
        LanguageId languageId,
        LessonId lessonId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.Exercises
            .AsNoTracking()
            .Where(exercise => exercise.LanguageId == languageId.Value && exercise.LessonId == lessonId.Value)
            .OrderBy(exercise => exercise.SortOrder)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task AddAsync(LanguageId languageId, Exercise exercise, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, exercise.LanguageId);
        db.Exercises.Add(exercise.ToRecord());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LanguageId languageId, Exercise exercise, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, exercise.LanguageId);
        var record = await db.Exercises.FirstOrDefaultAsync(
            row => row.LanguageId == languageId.Value && row.Id == exercise.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"Exercise {exercise.Id} was not found for this language.");
        exercise.CopyTo(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
