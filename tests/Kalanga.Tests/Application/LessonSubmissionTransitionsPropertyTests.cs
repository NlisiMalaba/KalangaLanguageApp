using CsCheck;
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

namespace Kalanga.Tests.Application;

[Collection(PostgresCollection.Name)]
public sealed class LessonSubmissionTransitionsPropertyTests(PostgresFixture postgres)
{
    private static readonly string[] Categories = ["Everyday", "Travel", "Family", "Work"];

    // Feature: kalanga-language-app, Property 32: Lesson Submission Transitions to PENDING_REVIEW
    [Fact(Timeout = 180_000)]
    public async Task Submitting_a_draft_moves_it_to_pending_review_and_not_the_catalog()
    {
        var draftInput =
            from title in Gen.String[Gen.Char['a', 'z'], 3, 40]
            from level in Gen.Enum<Level>()
            from category in Gen.OneOfConst(Categories)
            select (title, level, category);

        await Check.SampleAsync(
            draftInput,
            async (string title, Level level, string category) =>
            {
                await using var db = postgres.CreateDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                var languageId = LanguageId.New();
                await SeedLanguageAsync(db, languageId);

                var now = DateTimeOffset.UtcNow;
                var users = new UserRepository(db);
                var contributor = await SeedContributorAsync(users, languageId, now);
                var lessons = new LessonRepository(db);
                var create = new CreateLessonUseCase(users, lessons);
                var submit = new SubmitLessonForReviewUseCase(users, lessons);

                var created = await create.ExecuteAsync(
                    new CreateLessonCommand(languageId, contributor, title, level, category));
                Assert.Equal(LessonStatus.Draft, created.Status);

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
                    new BrowseLessonCatalogCommand(languageId, level, category, Skip: 0, Take: 50));
                Assert.DoesNotContain(catalog.Lessons, item => item.LessonId == created.LessonId);

                await Assert.ThrowsAsync<LessonNotFoundException>(() =>
                    new GetLessonUseCase(
                        lessons,
                        new PhraseRepository(db),
                        new LanguageVariationRepository(db),
                        new AudioRecordingRepository(db),
                        new ExerciseRepository(db)).ExecuteAsync(
                        new GetLessonCommand(languageId, created.LessonId)));

                await transaction.RollbackAsync();
            },
            iter: 100,
            threads: 1);
    }

    private static async Task<UserId> SeedContributorAsync(UserRepository users, LanguageId languageId, DateTimeOffset now)
    {
        var contributor = User.Register(
            languageId,
            $"contrib-{Guid.NewGuid():N}@example.com",
            "hash",
            "Contributor",
            now);
        contributor.ChangeRole(Role.Contributor, now);
        await users.AddAsync(languageId, contributor);
        return contributor.Id;
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
