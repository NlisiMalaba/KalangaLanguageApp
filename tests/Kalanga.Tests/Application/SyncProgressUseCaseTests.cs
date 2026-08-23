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
public sealed class SyncProgressUseCaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Push_applies_newer_progress_adds_xp_and_advances_checkpoint()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, published, phrase, sync, pull) = await SeedAsync(db);
        var clientTime = DateTimeOffset.UtcNow;

        var pushed = await sync.ExecuteAsync(new SyncProgressCommand(
            languageId,
            learner,
            Guid.NewGuid().ToString("N"),
            [
                new SyncProgressItemDto(published.Id, clientTime, Score: 91, XpAwarded: 10, clientTime),
            ],
            [
                new SyncSrsItemDto(
                    phrase.Id,
                    VariationId: null,
                    EaseFactor: 2.6m,
                    IntervalDays: 6,
                    Repetitions: 2,
                    NextReviewAt: DateOnly.FromDateTime(clientTime.UtcDateTime).AddDays(6),
                    LastReviewedAt: clientTime,
                    clientTime),
            ],
            new SyncGamificationDto(10, CurrentStreak: 3, LongestStreak: 5, LastActivityDate: DateOnly.FromDateTime(clientTime.UtcDateTime), Level.Beginner, XpDelta: 0, clientTime)));

        Assert.False(pushed.IdempotentReplay);
        Assert.Equal(1, pushed.SyncVersion);
        Assert.Equal(91, Assert.Single(pushed.ServerChanges.Progress).Score);
        Assert.Equal(10, pushed.ServerChanges.Gamification!.TotalXp);
        Assert.Equal(3, pushed.ServerChanges.Gamification.CurrentStreak);
        Assert.Equal(5, pushed.ServerChanges.Gamification.LongestStreak);
        Assert.Equal(6, Assert.Single(pushed.ServerChanges.SpacedRepetition).IntervalDays);

        var pulled = await pull.ExecuteAsync(new PullSyncCommand(languageId, learner, SinceVersion: 1));
        Assert.Equal(1, pulled.SyncVersion);
        Assert.Empty(pulled.ServerChanges.Progress);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Push_keeps_server_progress_when_incoming_updated_at_is_older()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, published, _, sync, _) = await SeedAsync(db);
        var newer = DateTimeOffset.UtcNow;
        var older = newer.AddMinutes(-10);

        await sync.ExecuteAsync(new SyncProgressCommand(
            languageId,
            learner,
            Guid.NewGuid().ToString("N"),
            [new SyncProgressItemDto(published.Id, newer, Score: 80, XpAwarded: 10, newer)],
            [],
            Gamification: null));

        var second = await sync.ExecuteAsync(new SyncProgressCommand(
            languageId,
            learner,
            Guid.NewGuid().ToString("N"),
            [new SyncProgressItemDto(published.Id, older, Score: 20, XpAwarded: 10, older)],
            [],
            new SyncGamificationDto(0, CurrentStreak: 1, LongestStreak: 1, LastActivityDate: DateOnly.FromDateTime(older.UtcDateTime), Level.Beginner, XpDelta: 4, older)));

        Assert.Equal(80, Assert.Single(second.ServerChanges.Progress).Score);
        Assert.Equal(14, second.ServerChanges.Gamification!.TotalXp);
        Assert.Equal(2, second.SyncVersion);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Identical_client_operation_does_not_award_xp_again()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, published, _, sync, _) = await SeedAsync(db);
        var clientTime = DateTimeOffset.UtcNow;
        var operationId = Guid.NewGuid().ToString("N");
        var command = new SyncProgressCommand(
            languageId,
            learner,
            operationId,
            [new SyncProgressItemDto(published.Id, clientTime, Score: 70, XpAwarded: 10, clientTime)],
            [],
            new SyncGamificationDto(10, 1, 1, DateOnly.FromDateTime(clientTime.UtcDateTime), Level.Beginner, XpDelta: 2, clientTime));

        var first = await sync.ExecuteAsync(command);
        var replay = await sync.ExecuteAsync(command);

        Assert.False(first.IdempotentReplay);
        Assert.True(replay.IdempotentReplay);
        Assert.Equal(first.SyncVersion, replay.SyncVersion);
        Assert.Equal(12, first.ServerChanges.Gamification!.TotalXp);
        Assert.Equal(12, replay.ServerChanges.Gamification!.TotalXp);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Pull_returns_server_changes_since_an_older_version()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, published, _, sync, pull) = await SeedAsync(db);
        var clientTime = DateTimeOffset.UtcNow;

        await sync.ExecuteAsync(new SyncProgressCommand(
            languageId,
            learner,
            Guid.NewGuid().ToString("N"),
            [new SyncProgressItemDto(published.Id, clientTime, Score: 64, XpAwarded: 10, clientTime)],
            [],
            Gamification: null));

        var pulled = await pull.ExecuteAsync(new PullSyncCommand(languageId, learner, SinceVersion: 0));
        Assert.Equal(64, Assert.Single(pulled.ServerChanges.Progress).Score);
        Assert.Equal(1, pulled.SyncVersion);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Suspended_user_cannot_sync()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var (languageId, learner, published, _, sync, pull) = await SeedAsync(db);
        var users = new UserRepository(db);
        var actor = await users.FindByIdAsync(languageId, learner);
        Assert.NotNull(actor);
        actor.Suspend(DateTimeOffset.UtcNow);
        await users.UpdateAsync(languageId, actor);

        await Assert.ThrowsAsync<UserSuspendedException>(() =>
            sync.ExecuteAsync(new SyncProgressCommand(
                languageId,
                learner,
                Guid.NewGuid().ToString("N"),
                [new SyncProgressItemDto(published.Id, DateTimeOffset.UtcNow, 50, 10, DateTimeOffset.UtcNow)],
                [],
                Gamification: null)));
        await Assert.ThrowsAsync<UserSuspendedException>(() =>
            pull.ExecuteAsync(new PullSyncCommand(languageId, learner, SinceVersion: 0)));
        await Assert.ThrowsAsync<InvalidSyncPayloadException>(() =>
            sync.ExecuteAsync(new SyncProgressCommand(languageId, learner, " ", [], [], null)));

        await transaction.RollbackAsync();
    }

    private static async Task<(
        LanguageId LanguageId,
        UserId Learner,
        Lesson Published,
        Phrase Phrase,
        SyncProgressUseCase Sync,
        PullSyncUseCase Pull)> SeedAsync(KalangaDbContext db)
    {
        var languageId = LanguageId.New();
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

        var now = DateTimeOffset.UtcNow;
        var users = new UserRepository(db);
        var learnerUser = User.Register(languageId, $"learner-{Guid.NewGuid():N}@example.com", "hash", "Learner", now);
        var contributor = User.Register(languageId, $"contrib-{Guid.NewGuid():N}@example.com", "hash", "Contributor", now);
        contributor.ChangeRole(Role.Contributor, now);
        var reviewer = User.Register(languageId, $"review-{Guid.NewGuid():N}@example.com", "hash", "Reviewer", now);
        reviewer.ChangeRole(Role.Reviewer, now);
        await users.AddAsync(languageId, learnerUser);
        await users.AddAsync(languageId, contributor);
        await users.AddAsync(languageId, reviewer);

        var lessons = new LessonRepository(db);
        var published = Lesson.CreateDraft(languageId, contributor.Id, "Greetings", Level.Beginner, "Everyday", now);
        published.SubmitForReview(now);
        published.Approve(reviewer.Id, now);
        await lessons.AddAsync(languageId, published);

        var phrases = new PhraseRepository(db);
        var phrase = Phrase.Create(languageId, published.Id, "Ndini", "I am", sortOrder: 0, now);
        await phrases.AddAsync(languageId, phrase);

        var variations = new LanguageVariationRepository(db);
        var progress = new LearnerProgressRepository(db);
        var srs = new SpacedRepetitionRepository(db);
        var gamification = new GamificationRepository(db);
        var checkpoints = new SyncCheckpointRepository(db);
        var receipts = new SyncPushReceiptRepository(db);
        var unitOfWork = new EfUnitOfWork(db);

        var sync = new SyncProgressUseCase(
            users,
            lessons,
            phrases,
            variations,
            progress,
            srs,
            gamification,
            checkpoints,
            receipts,
            unitOfWork);
        var pull = new PullSyncUseCase(users, progress, srs, gamification, checkpoints);
        return (languageId, learnerUser.Id, published, phrase, sync, pull);
    }
}
