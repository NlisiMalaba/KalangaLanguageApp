using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public interface IRequestRepository
{
    Task<Request?> FindByIdAsync(LanguageId languageId, RequestId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Request>> FindOpenSortedByUpvotesAsync(
        LanguageId languageId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(LanguageId languageId, Request request, CancellationToken cancellationToken = default);

    Task UpdateAsync(LanguageId languageId, Request request, CancellationToken cancellationToken = default);

    Task<bool> AddUpvoteAsync(
        LanguageId languageId,
        RequestId requestId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
