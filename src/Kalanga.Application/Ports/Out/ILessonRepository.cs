using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface ILessonRepository
{
    Task<Lesson?> FindByIdAsync(LanguageId languageId, LessonId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lesson>> FindByIdsAsync(
        LanguageId languageId,
        IReadOnlyCollection<LessonId> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lesson>> FindPublishedAsync(
        LanguageId languageId,
        Level? level,
        string? category,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lesson>> FindPendingReviewAsync(
        LanguageId languageId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lesson>> FindByContributorAsync(
        LanguageId languageId,
        UserId contributorId,
        LessonStatus? status,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, Lesson lesson, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, Lesson lesson, CancellationToken cancellationToken = default);
}
