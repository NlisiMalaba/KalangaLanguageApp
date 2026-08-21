using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(KalangaDbContext db) : IUserRepository
{
    public async Task<User?> FindByIdAsync(LanguageId languageId, UserId id, CancellationToken cancellationToken = default)
    {
        var record = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.LanguageId == languageId.Value && user.Id == id.Value, cancellationToken);
        return record?.ToDomain();
    }

    public async Task<User?> FindByEmailAsync(LanguageId languageId, string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var record = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.LanguageId == languageId.Value && user.Email == normalized,
                cancellationToken);
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<User>> ListAsync(
        LanguageId languageId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        TenantGuard.EnsureSkip(skip);
        take = TenantGuard.ClampTake(take);

        var records = await db.Users
            .AsNoTracking()
            .Where(user => user.LanguageId == languageId.Value)
            .OrderBy(user => user.Email)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task AddAsync(LanguageId languageId, User user, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, user.LanguageId);
        db.Users.Add(user.ToRecord());

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueEmailViolation(ex))
        {
            throw new DuplicateEmailException(user.Email);
        }
    }

    private static bool IsUniqueEmailViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgres
            || postgres.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        return postgres.ConstraintName?.Contains("email", StringComparison.OrdinalIgnoreCase) == true
            || postgres.MessageText.Contains("email", StringComparison.OrdinalIgnoreCase);
    }

    public async Task UpdateAsync(LanguageId languageId, User user, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, user.LanguageId);
        var record = await db.Users.FirstOrDefaultAsync(
            row => row.LanguageId == languageId.Value && row.Id == user.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"User {user.Id} was not found for this language.");
        user.CopyTo(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
