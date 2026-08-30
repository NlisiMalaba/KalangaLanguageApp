using Kalanga.Application.Dtos;
using Kalanga.Application.UseCases;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class CreateLanguageUseCaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Admin_can_create_a_language_and_list_returns_it()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (actorLanguageId, admin) = await SeedAdminAsync(db);
        var languages = new LanguageRepository(db);
        var create = new CreateLanguageUseCase(
            new UserRepository(db),
            languages,
            NullLogger<CreateLanguageUseCase>.Instance);
        var list = new ListLanguagesUseCase(languages);

        var created = await create.ExecuteAsync(
            new CreateLanguageCommand(actorLanguageId, admin, "nde", "Ndebele", "Zimbabwe"));

        Assert.Equal("nde", created.Language.Code);
        Assert.Equal("Ndebele", created.Language.Name);
        Assert.True(created.Language.IsActive);
        Assert.NotEqual(Guid.Empty, created.Language.LanguageId.Value);

        var listed = await list.ExecuteAsync();
        Assert.Contains(listed.Languages, item => item.LanguageId == created.Language.LanguageId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Duplicate_language_code_is_rejected()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (actorLanguageId, admin) = await SeedAdminAsync(db);
        var create = new CreateLanguageUseCase(
            new UserRepository(db),
            new LanguageRepository(db),
            NullLogger<CreateLanguageUseCase>.Instance);

        var command = new CreateLanguageCommand(actorLanguageId, admin, "sna", "Shona", "Zimbabwe");
        await create.ExecuteAsync(command);

        await Assert.ThrowsAsync<DuplicateLanguageCodeException>(() => create.ExecuteAsync(command));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Contributor_cannot_create_a_language()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var users = new UserRepository(db);
        var contributor = User.Register(
            languageId,
            $"contrib-{Guid.NewGuid():N}@example.com",
            "hash",
            "Contributor",
            DateTimeOffset.UtcNow);
        contributor.ChangeRole(Role.Contributor, DateTimeOffset.UtcNow);
        await users.AddAsync(languageId, contributor);

        var create = new CreateLanguageUseCase(
            users,
            new LanguageRepository(db),
            NullLogger<CreateLanguageUseCase>.Instance);

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            create.ExecuteAsync(
                new CreateLanguageCommand(languageId, contributor.Id, "xho", "Xhosa", "South Africa")));

        await transaction.RollbackAsync();
    }

    private static async Task<(LanguageId LanguageId, UserId AdminId)> SeedAdminAsync(KalangaDbContext db)
    {
        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var users = new UserRepository(db);
        var admin = User.Register(
            languageId,
            $"admin-{Guid.NewGuid():N}@example.com",
            "hash",
            "Admin",
            DateTimeOffset.UtcNow);
        admin.ChangeRole(Role.Admin, DateTimeOffset.UtcNow);
        await users.AddAsync(languageId, admin);
        return (languageId, admin.Id);
    }

    private static async Task SeedLanguageAsync(KalangaDbContext db, LanguageId languageId)
    {
        db.Languages.Add(new LanguageRecord
        {
            Id = languageId.Value,
            Code = languageId.Value.ToString("N")[^10..],
            Name = "Test Language",
            Region = "Test",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
