using CsCheck;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.Out;
using Kalanga.Application.UseCases;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class DefaultLearnerRolePropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 2: Default Learner Role on Registration
    [Fact(Timeout = 180_000)]
    public async Task Registration_always_assigns_learner_role_and_active_status()
    {
        var registrationInput =
            from local in Gen.String[Gen.Char['a', 'z'], 3, 24]
            from display in Gen.String[Gen.Char['a', 'z'], 2, 40]
            from password in Gen.String[Gen.Char.AlphaNumeric, 8, 32]
            select (
                Email: $"{local}-{Guid.NewGuid():N}@example.com",
                DisplayName: display,
                Password: password);

        await Check.SampleAsync(
            registrationInput,
            async (string email, string displayName, string password) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);

                var users = new UserRepository(db);
                var useCase = new RegisterUserUseCase(users, new LanguageRepository(db), new FastPasswordHasher());

                var result = await useCase.ExecuteAsync(
                    new RegisterUserCommand(languageId, email, password, displayName));

                Assert.Equal(Role.Learner, result.Role);

                var persisted = await users.FindByIdAsync(languageId, result.UserId);
                Assert.NotNull(persisted);
                Assert.Equal(Role.Learner, persisted.Role);
                Assert.Equal(UserStatus.Active, persisted.Status);

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
