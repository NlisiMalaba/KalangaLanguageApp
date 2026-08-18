using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface IExerciseRepository
{
    Task<Exercise?> FindByIdAsync(LanguageId languageId, ExerciseId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Exercise>> FindByLessonIdAsync(
        LanguageId languageId,
        LessonId lessonId,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, Exercise exercise, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, Exercise exercise, CancellationToken cancellationToken = default);
}
