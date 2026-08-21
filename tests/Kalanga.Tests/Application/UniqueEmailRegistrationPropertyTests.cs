using CsCheck;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.Out;
using Kalanga.Application.UseCases;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class UniqueEmailRegistrationPropertyTests(PostgresFixture postgres)
{
    // Feature: kalanga-language-app, Property 1: Unique Email Registration Invariant
    [Fact(Timeout = 180_000)]
    public async Task Duplicate_email_registration_fails_and_user_count_is_unchanged()
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
                var useCase = new RegisterUserUseCase(users, new FastPasswordHasher());
                var command = new RegisterUserCommand(languageId, email, password, displayName);

                var first = await useCase.ExecuteAsync(command);
                Assert.Equal(email.ToLowerInvariant(), first.Email);

                var countAfterFirst = await db.Users.CountAsync(user => user.LanguageId == languageId.Value);
                Assert.Equal(1, countAfterFirst);

                var duplicate = await Assert.ThrowsAsync<DuplicateEmailException>(
                    () => useCase.ExecuteAsync(command));
                Assert.Equal(email, duplicate.Email);

                var countAfterDuplicate = await db.Users.CountAsync(user => user.LanguageId == languageId.Value);
                Assert.Equal(countAfterFirst, countAfterDuplicate);

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

    /// <summary>
    /// Deterministic hasher so the uniqueness property is not dominated by BCrypt cost.
    /// </summary>
    private sealed class FastPasswordHasher : IPasswordHasher
    {
        public string TimingPadHash { get; } = "timing-pad";

        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }
}
