using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class LanguageRepository(KalangaDbContext db) : ILanguageRepository
{
    public async Task<Language?> FindByIdAsync(LanguageId id, CancellationToken cancellationToken = default)
    {
        var record = await db.Languages
            .AsNoTracking()
            .FirstOrDefaultAsync(language => language.Id == id.Value, cancellationToken);
        return record?.ToDomain();
    }

    public async Task<Language?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToLowerInvariant();
        var record = await db.Languages
            .AsNoTracking()
            .FirstOrDefaultAsync(language => language.Code == normalized, cancellationToken);
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Language>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        var records = await db.Languages
            .AsNoTracking()
            .Where(language => language.IsActive)
            .OrderBy(language => language.Name)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task AddAsync(Language language, CancellationToken cancellationToken = default)
    {
        db.Languages.Add(language.ToRecord());

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueCodeViolation(ex))
        {
            throw new DuplicateLanguageCodeException(language.Code);
        }
    }

    private static bool IsUniqueCodeViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgres
            || postgres.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        return postgres.ConstraintName?.Contains("code", StringComparison.OrdinalIgnoreCase) == true
            || postgres.MessageText.Contains("ix_languages_code", StringComparison.OrdinalIgnoreCase);
    }
}
