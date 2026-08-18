using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface IGamificationRepository
{
    Task<LearnerGamification?> FindByUserAsync(
        LanguageId languageId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, LearnerGamification gamification, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, LearnerGamification gamification, CancellationToken cancellationToken = default);
}
