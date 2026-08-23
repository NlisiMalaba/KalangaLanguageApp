using System.IdentityModel.Tokens.Jwt;
using CsCheck;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.Out;
using Kalanga.Application.UseCases;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Infrastructure.Security;
using Kalanga.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class RoleUpdateTakesEffectPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 3: Role Update Takes Effect Immediately
    [Fact(Timeout = 180_000)]
    public async Task Role_change_is_persisted_and_reflected_in_subsequent_auth_token()
    {
        var input =
            from local in Gen.String[Gen.Char['a', 'z'], 3, 20]
            from display in Gen.String[Gen.Char['a', 'z'], 2, 30]
            from password in Gen.String[Gen.Char.AlphaNumeric, 8, 24]
            from newRole in Gen.Enum<Role>()
            select (
                Email: $"{local}-{Guid.NewGuid():N}@example.com",
                DisplayName: display,
                Password: password,
                NewRole: newRole);

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
            async (string email, string displayName, string password, Role newRole) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);

                var users = new UserRepository(db);
                var refreshTokens = new RefreshTokenRepository(db);
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

                var manage = new ManageUserRoleUseCase(users);
                var manageResult = await manage.ExecuteAsync(
                    new ManageUserRoleCommand(
                        languageId,
                        admin.Id,
                        target.Id,
                        ManageUserAction.ChangeRole,
                        newRole));

                Assert.Equal(newRole, manageResult.Role);

                var persisted = await users.FindByIdAsync(languageId, target.Id);
                Assert.NotNull(persisted);
                Assert.Equal(newRole, persisted.Role);

                var authenticate = new AuthenticateUserUseCase(users, hasher, tokenService, refreshTokens);
                var authResult = await authenticate.ExecuteAsync(
                    new AuthenticateUserCommand(languageId, email, password));

                Assert.Equal(newRole, authResult.Role);

                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(authResult.AccessToken);
                var roleClaim = jwt.Claims.First(claim => claim.Type is "role" or "Role").Value;
                Assert.Equal(newRole.ToString(), roleClaim);

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
            Code = languageId.Value.ToString("N")[^10..],
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
