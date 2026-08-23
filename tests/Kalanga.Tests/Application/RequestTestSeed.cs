using Kalanga.Application.UseCases;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Kalanga.Infrastructure.Persistence.Repositories;

namespace Kalanga.Tests.Application;

internal static class RequestTestSeed
{
    public static async Task<RequestTestContext> CreateAsync(KalangaDbContext db, int extraVoters = 0)
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
        var learner = User.Register(languageId, $"learner-{Guid.NewGuid():N}@example.com", "hash", "Learner", now);
        var contributor = User.Register(languageId, $"contrib-{Guid.NewGuid():N}@example.com", "hash", "Contributor", now);
        contributor.ChangeRole(Role.Contributor, now);
        var reviewer = User.Register(languageId, $"review-{Guid.NewGuid():N}@example.com", "hash", "Reviewer", now);
        reviewer.ChangeRole(Role.Reviewer, now);
        await users.AddAsync(languageId, learner);
        await users.AddAsync(languageId, contributor);
        await users.AddAsync(languageId, reviewer);

        var voters = new List<UserId> { learner.Id };
        for (var i = 0; i < extraVoters; i++)
        {
            var voter = User.Register(languageId, $"voter-{i}-{Guid.NewGuid():N}@example.com", "hash", $"Voter{i}", now);
            await users.AddAsync(languageId, voter);
            voters.Add(voter.Id);
        }

        var lessons = new LessonRepository(db);
        var lesson = Lesson.CreateDraft(languageId, contributor.Id, "Greetings", Level.Beginner, "Everyday", now);
        lesson.SubmitForReview(now);
        lesson.Approve(reviewer.Id, now);
        await lessons.AddAsync(languageId, lesson);

        var requests = new RequestRepository(db);
        return new RequestTestContext(
            languageId,
            learner.Id,
            contributor.Id,
            lesson,
            voters,
            new SubmitRequestUseCase(users, requests),
            new UpvoteRequestUseCase(users, requests),
            new ListRequestsUseCase(users, requests),
            new FulfillRequestUseCase(
                users,
                requests,
                lessons,
                new NotificationOutbox(db),
                new EfUnitOfWork(db)));
    }
}

internal sealed record RequestTestContext(
    LanguageId LanguageId,
    UserId Learner,
    UserId Contributor,
    Lesson Lesson,
    IReadOnlyList<UserId> Voters,
    SubmitRequestUseCase Submit,
    UpvoteRequestUseCase Upvote,
    ListRequestsUseCase List,
    FulfillRequestUseCase Fulfill);
