using Kalanga.Application.Dtos;
using Kalanga.Application.UseCases;
using Kalanga.Domain;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Tests.Infrastructure;

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class CreateAndSubmitLessonUseCaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Contributor_creates_draft_that_is_not_in_the_published_catalog()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now);
        var lessons = new LessonRepository(db);

        var created = await new CreateLessonUseCase(users, lessons).ExecuteAsync(
            new CreateLessonCommand(
                languageId,
                contributor,
                "Greetings",
                Level.Beginner,
                "Everyday",
                IsScenario: true,
                ScenarioContext: "Meeting a neighbour"));

        Assert.Equal(LessonStatus.Draft, created.Status);
        Assert.Equal(contributor, created.ContributorId);
        Assert.Equal(DomainRules.DefaultLessonXpReward, created.XpReward);
        Assert.True(created.IsScenario);

        var persisted = await lessons.FindByIdAsync(languageId, created.LessonId);
        Assert.NotNull(persisted);
        Assert.Equal(LessonStatus.Draft, persisted.Status);
        Assert.False(persisted.IsVisibleInCatalog);

        var catalog = await new BrowseLessonCatalogUseCase(lessons).ExecuteAsync(
            new BrowseLessonCatalogCommand(languageId, Level: null, Category: null, Skip: 0, Take: 50));
        Assert.Empty(catalog.Lessons);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Learner_cannot_create_a_lesson()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var users = new UserRepository(db);
        var learner = await SeedUserAsync(users, languageId, Role.Learner, DateTimeOffset.UtcNow);

        var thrown = await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            new CreateLessonUseCase(users, new LessonRepository(db)).ExecuteAsync(
                new CreateLessonCommand(languageId, learner, "Greetings", Level.Beginner, "Everyday")));

        Assert.Equal(Role.Contributor, thrown.RequiredRole);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Submit_moves_draft_to_pending_review_and_keeps_it_out_of_the_catalog()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now);
        var lessons = new LessonRepository(db);
        var create = new CreateLessonUseCase(users, lessons);
        var submit = new SubmitLessonForReviewUseCase(users, lessons);

        var created = await create.ExecuteAsync(
            new CreateLessonCommand(languageId, contributor, "Market phrases", Level.Intermediate, "Travel"));

        var submitted = await submit.ExecuteAsync(
            new SubmitLessonForReviewCommand(languageId, contributor, created.LessonId));

        Assert.Equal(LessonStatus.PendingReview, submitted.Status);

        var persisted = await lessons.FindByIdAsync(languageId, created.LessonId);
        Assert.NotNull(persisted);
        Assert.Equal(LessonStatus.PendingReview, persisted.Status);
        Assert.False(persisted.IsVisibleInCatalog);

        var queue = await lessons.FindPendingReviewAsync(languageId, skip: 0, take: 50);
        Assert.Contains(queue, lesson => lesson.Id == created.LessonId);

        var catalog = await new BrowseLessonCatalogUseCase(lessons).ExecuteAsync(
            new BrowseLessonCatalogCommand(languageId, Level: null, Category: null, Skip: 0, Take: 50));
        Assert.DoesNotContain(catalog.Lessons, item => item.LessonId == created.LessonId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Contributor_cannot_submit_another_contributors_draft()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var owner = await SeedUserAsync(users, languageId, Role.Contributor, now, "owner");
        var other = await SeedUserAsync(users, languageId, Role.Contributor, now, "other");
        var lessons = new LessonRepository(db);

        var created = await new CreateLessonUseCase(users, lessons).ExecuteAsync(
            new CreateLessonCommand(languageId, owner, "Greetings", Level.Beginner, "Everyday"));

        await Assert.ThrowsAsync<LessonAccessDeniedException>(() =>
            new SubmitLessonForReviewUseCase(users, lessons).ExecuteAsync(
                new SubmitLessonForReviewCommand(languageId, other, created.LessonId)));

        var persisted = await lessons.FindByIdAsync(languageId, created.LessonId);
        Assert.Equal(LessonStatus.Draft, persisted!.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Admin_can_create_and_submit_a_draft()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var admin = await SeedUserAsync(users, languageId, Role.Admin, now);
        var lessons = new LessonRepository(db);

        var created = await new CreateLessonUseCase(users, lessons).ExecuteAsync(
            new CreateLessonCommand(languageId, admin, "Admin draft", Level.Advanced, "Work", XpReward: 25));
        Assert.Equal(LessonStatus.Draft, created.Status);
        Assert.Equal(25, created.XpReward);

        var submitted = await new SubmitLessonForReviewUseCase(users, lessons).ExecuteAsync(
            new SubmitLessonForReviewCommand(languageId, admin, created.LessonId));
        Assert.Equal(LessonStatus.PendingReview, submitted.Status);

        await transaction.RollbackAsync();
    }

    private static async Task<UserId> SeedUserAsync(
        UserRepository users,
        LanguageId languageId,
        Role role,
        DateTimeOffset now,
        string? suffix = null)
    {
        var user = User.Register(
            languageId,
            $"{role.ToString().ToLowerInvariant()}-{suffix}-{Guid.NewGuid():N}@example.com",
            "hash",
            role.ToString(),
            now);
        if (role != Role.Learner)
        {
            user.ChangeRole(role, now);
        }

        await users.AddAsync(languageId, user);
        return user.Id;
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
}
