using CsCheck;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.Out;
using Kalanga.Application.UseCases;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Infrastructure.Security;
using Kalanga.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class UserManagementStateTransitionsPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 41: User Management State Transitions
    [Fact(Timeout = 180_000)]
    public async Task Suspend_blocks_auth_reactivate_restores_auth_and_list_includes_all_statuses()
    {
        var input =
            from local in Gen.String[Gen.Char['a', 'z'], 3, 20]
            from display in Gen.String[Gen.Char['a', 'z'], 2, 30]
            from password in Gen.String[Gen.Char.AlphaNumeric, 8, 24]
            select (
                Email: $"{local}-{Guid.NewGuid():N}@example.com",
                DisplayName: display,
                Password: password);

        var hasher = new FastPasswordHasher();
        var tokenService = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "kalanga-test",
            Audience = "kalanga-test-clients",
            SigningKey = "test-signing-key-at-least-32-bytes!!",
            AccessTokenHours = 24,
            RefreshTokenDays = 30,
        }));

        await Check.SampleAsync(
            input,
            async (string email, string displayName, string password) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);

                var users = new UserRepository(db);
                var refreshTokens = new RefreshTokenRepository(db);
                var manage = new ManageUserRoleUseCase(users);
                var authenticate = new AuthenticateUserUseCase(users, hasher, tokenService, refreshTokens);
                var now = DateTimeOffset.UtcNow;

                var admin = User.Register(
                    languageId,
                    $"admin-{Guid.NewGuid():N}@example.com",
                    hasher.Hash("admin-pass-1"),
                    "Admin",
                    now);
                admin.ChangeRole(Role.Admin, now);
                await users.AddAsync(languageId, admin);

                var target = User.Register(languageId, email, hasher.Hash(password), displayName, now);
                await users.AddAsync(languageId, target);

                await manage.ExecuteAsync(
                    new ManageUserRoleCommand(
                        languageId,
                        admin.Id,
                        target.Id,
                        ManageUserAction.Suspend,
                        NewRole: null));

                var suspended = await users.FindByIdAsync(languageId, target.Id);
                Assert.NotNull(suspended);
                Assert.Equal(UserStatus.Suspended, suspended.Status);

                await Assert.ThrowsAsync<UserSuspendedException>(() =>
                    authenticate.ExecuteAsync(new AuthenticateUserCommand(languageId, email, password)));

                await manage.ExecuteAsync(
                    new ManageUserRoleCommand(
                        languageId,
                        admin.Id,
                        target.Id,
                        ManageUserAction.Reactivate,
                        NewRole: null));

                var reactivated = await users.FindByIdAsync(languageId, target.Id);
                Assert.NotNull(reactivated);
                Assert.Equal(UserStatus.Active, reactivated.Status);

                var authResult = await authenticate.ExecuteAsync(
                    new AuthenticateUserCommand(languageId, email, password));
                Assert.Equal(target.Id, authResult.UserId);

                // Suspend again so the admin list must include both Active (admin) and Suspended (target).
                await manage.ExecuteAsync(
                    new ManageUserRoleCommand(
                        languageId,
                        admin.Id,
                        target.Id,
                        ManageUserAction.Suspend,
                        NewRole: null));

                var listed = await users.ListAsync(languageId, skip: 0, take: 50);
                Assert.Contains(listed, user => user.Id == admin.Id && user.Status == UserStatus.Active);
                Assert.Contains(listed, user => user.Id == target.Id && user.Status == UserStatus.Suspended);
                Assert.Equal(2, listed.Count);

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static async Task SeedLanguageAsync(KalangaDbContext db, LanguageId languageId)
    {
        db.Languages.Add(new LanguageRecord
        {
            Id = languageId.Value,
            Code = languageId.Value.ToString("N")[..10],
            Name = "Test Language",
            Region = "Test",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private sealed class FastPasswordHasher : IPasswordHasher
    {
        public string TimingPadHash { get; } = "timing-pad";

        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }
}
