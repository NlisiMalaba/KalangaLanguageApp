using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface IPhraseRepository
{
    Task<Phrase?> FindByIdAsync(LanguageId languageId, PhraseId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Phrase>> FindByLessonIdAsync(
        LanguageId languageId,
        LessonId lessonId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Phrase>> FindByLessonIdsAsync(
        LanguageId languageId,
        IReadOnlyCollection<LessonId> lessonIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, Phrase phrase, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, Phrase phrase, CancellationToken cancellationToken = default);
}
