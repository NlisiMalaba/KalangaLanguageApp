using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface ILearnerProgressRepository
{
    Task<LearnerProgress?> FindByUserAndLessonAsync(
        LanguageId languageId,
        UserId userId,
        LessonId lessonId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LearnerProgress>> FindByUserAsync(
        LanguageId languageId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LearnerProgress>> FindUpdatedSinceAsync(
        LanguageId languageId,
        UserId userId,
        DateTimeOffset updatedAfter,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, LearnerProgress progress, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, LearnerProgress progress, CancellationToken cancellationToken = default);
}
