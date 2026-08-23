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
public sealed class CompleteLessonAndGetProgressUseCaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Completing_a_published_lesson_records_progress_and_awards_xp_once()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, published, other, complete, getProgress) = await SeedAsync(db);

        var first = await complete.ExecuteAsync(
            new CompleteLessonCommand(languageId, learner, published.Id, Score: 88));

        Assert.True(first.XpGranted);
        Assert.Equal(published.XpReward, first.XpAwarded);
        Assert.Equal(published.XpReward, first.TotalXp);
        Assert.Equal(88, first.Score);

        var second = await complete.ExecuteAsync(
            new CompleteLessonCommand(languageId, learner, published.Id, Score: 40));

        Assert.False(second.XpGranted);
        Assert.Equal(40, second.Score);
        Assert.Equal(published.XpReward, second.XpAwarded);
        Assert.Equal(published.XpReward, second.TotalXp);

        var progress = await getProgress.ExecuteAsync(new GetProgressCommand(languageId, learner));
        Assert.Equal(published.XpReward, progress.TotalXp);

        var beginner = Assert.Single(progress.ByLevel, item => item.Level == Level.Beginner);
        Assert.Equal(1, beginner.CompletedCount);
        Assert.Equal(2, beginner.TotalCount);
        Assert.Equal(50.0m, beginner.Percentage);

        var everyday = Assert.Single(progress.ByCategory, item => item.Category == "Everyday");
        Assert.Equal(1, everyday.CompletedCount);
        Assert.Equal(2, everyday.TotalCount);

        var weak = Assert.Single(progress.WeakAreas);
        Assert.Equal(Level.Beginner, weak.Level);
        Assert.Equal("Everyday", weak.Category);
        Assert.Equal(40.0m, weak.AverageScore);
        Assert.True(weak.AverageScore < DomainRules.WeakAreaScoreThreshold);

        var persisted = await new LearnerProgressRepository(db).FindByUserAndLessonAsync(
            languageId,
            learner,
            published.Id);
        Assert.NotNull(persisted);
        Assert.True(persisted.IsCompleted);
        Assert.Equal(40, persisted.Score);

        Assert.NotEqual(published.Id, other.Id);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Unpublished_lesson_cannot_be_completed()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var learner = await SeedUserAsync(users, languageId, Role.Learner, now, "l");
        var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now, "c");
        var lessons = new LessonRepository(db);
        var draft = Lesson.CreateDraft(languageId, contributor, "Draft", Level.Beginner, "Everyday", now);
        await lessons.AddAsync(languageId, draft);

        var complete = new CompleteLessonUseCase(
            users,
            lessons,
            new LearnerProgressRepository(db),
            new GamificationRepository(db));

        await Assert.ThrowsAsync<LessonNotFoundException>(() =>
            complete.ExecuteAsync(new CompleteLessonCommand(languageId, learner, draft.Id, Score: 100)));

        await Assert.ThrowsAsync<InvalidLessonCompletionException>(() =>
            complete.ExecuteAsync(new CompleteLessonCommand(languageId, learner, draft.Id, Score: 101)));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task High_scores_are_not_weak_areas()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, published, _, complete, getProgress) = await SeedAsync(db);
        await complete.ExecuteAsync(new CompleteLessonCommand(languageId, learner, published.Id, Score: 95));

        var progress = await getProgress.ExecuteAsync(new GetProgressCommand(languageId, learner));
        Assert.Empty(progress.WeakAreas);
        Assert.Equal(50.0m, Assert.Single(progress.ByLevel).Percentage);

        await transaction.RollbackAsync();
    }

    private static async Task<(
        LanguageId LanguageId,
        UserId Learner,
        Lesson Published,
        Lesson Other,
        CompleteLessonUseCase Complete,
        GetProgressUseCase GetProgress)> SeedAsync(KalangaDbContext db)
    {
        var languageId = LanguageId.New();
        await SeedLanguageAsync(db, languageId);
        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var learner = await SeedUserAsync(users, languageId, Role.Learner, now, "l");
        var contributor = await SeedUserAsync(users, languageId, Role.Contributor, now, "c");
        var reviewer = await SeedUserAsync(users, languageId, Role.Reviewer, now, "r");
        var lessons = new LessonRepository(db);

        var published = Lesson.CreateDraft(languageId, contributor, "Greetings", Level.Beginner, "Everyday", now);
        published.SubmitForReview(now);
        published.Approve(reviewer, now);
        await lessons.AddAsync(languageId, published);

        var other = Lesson.CreateDraft(languageId, contributor, "Travel", Level.Beginner, "Everyday", now);
        other.SubmitForReview(now);
        other.Approve(reviewer, now);
        await lessons.AddAsync(languageId, other);

        var progress = new LearnerProgressRepository(db);
        var gamification = new GamificationRepository(db);
        var complete = new CompleteLessonUseCase(users, lessons, progress, gamification);
        var getProgress = new GetProgressUseCase(users, lessons, progress, gamification);
        return (languageId, learner, published, other, complete, getProgress);
    }

    private static async Task<UserId> SeedUserAsync(
        UserRepository users,
        LanguageId languageId,
        Role role,
        DateTimeOffset now,
        string suffix)
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
            Code = languageId.Value.ToString("N")[^10..],
            Name = "Test Language",
            Region = "Test",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
