using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class RequestRepository(KalangaDbContext db) : IRequestRepository
{
    public async Task<Request?> FindByIdAsync(LanguageId languageId, RequestId id, CancellationToken cancellationToken = default)
    {
        var record = await db.Requests
            .AsNoTracking()
            .FirstOrDefaultAsync(request => request.LanguageId == languageId.Value && request.Id == id.Value, cancellationToken);
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Request>> FindOpenSortedByUpvotesAsync(
        LanguageId languageId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        TenantGuard.EnsureSkip(skip);
        take = TenantGuard.ClampTake(take);
        var open = RequestStatus.Open.ToString();

        var records = await db.Requests
            .AsNoTracking()
            .Where(request => request.LanguageId == languageId.Value && request.Status == open)
            .OrderByDescending(request => request.UpvoteCount)
            .ThenBy(request => request.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task AddAsync(LanguageId languageId, Request request, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, request.LanguageId);
        db.Requests.Add(request.ToRecord());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LanguageId languageId, Request request, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, request.LanguageId);
        var record = await db.Requests.FirstOrDefaultAsync(
            row => row.LanguageId == languageId.Value && row.Id == request.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"Request {request.Id} was not found for this language.");
        request.CopyTo(record);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AddUpvoteAsync(
        LanguageId languageId,
        RequestId requestId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var open = RequestStatus.Open.ToString();
        var exists = await db.Requests.AnyAsync(
            request => request.LanguageId == languageId.Value
                       && request.Id == requestId.Value
                       && request.Status == open,
            cancellationToken);
        if (!exists)
        {
            return false;
        }

        var alreadyVoted = await db.RequestUpvotes.AnyAsync(
            upvote => upvote.RequestId == requestId.Value && upvote.UserId == userId.Value,
            cancellationToken);
        if (alreadyVoted)
        {
            return false;
        }

        db.RequestUpvotes.Add(new RequestUpvoteRecord
        {
            RequestId = requestId.Value,
            UserId = userId.Value,
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await db.Requests
                .Where(request => request.LanguageId == languageId.Value
                                  && request.Id == requestId.Value
                                  && request.Status == open)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(request => request.UpvoteCount, request => request.UpvoteCount + 1),
                    cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres
        && postgres.SqlState == PostgresErrorCodes.UniqueViolation;
}
