using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Mapping;

namespace Kalanga.Tests.Infrastructure;

public sealed class RecordMapperTests
{
    [Fact]
    public void User_round_trips_through_persistence_record()
    {
        var original = User.Register(LanguageId.New(), "a@b.co", "hash", "Ama", DateTimeOffset.UtcNow);
        original.ChangeRole(Role.Contributor, DateTimeOffset.UtcNow);

        var restored = original.ToRecord().ToDomain();

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.LanguageId, restored.LanguageId);
        Assert.Equal(original.Email, restored.Email);
        Assert.Equal(Role.Contributor, restored.Role);
        Assert.Equal(UserStatus.Active, restored.Status);
    }

    [Fact]
    public void Lesson_round_trips_status_and_reviewer()
    {
        var now = DateTimeOffset.UtcNow;
        var lesson = Lesson.CreateDraft(LanguageId.New(), UserId.New(), "Greetings", Level.Beginner, "Everyday", now);
        lesson.SubmitForReview(now);
        var reviewer = UserId.New();
        lesson.Approve(reviewer, now);

        var restored = lesson.ToRecord().ToDomain();

        Assert.Equal(LessonStatus.Published, restored.Status);
        Assert.Equal(reviewer, restored.ReviewedBy);
        Assert.True(restored.IsVisibleInCatalog);
    }

    [Fact]
    public void Language_variation_round_trips_through_persistence_record()
    {
        var original = LanguageVariation.Create(
            LanguageId.New(),
            PhraseId.New(),
            "Ndini",
            "formal",
            DateTimeOffset.UtcNow);

        var restored = original.ToRecord().ToDomain();

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.PhraseId, restored.PhraseId);
        Assert.Equal(original.KalangaText, restored.KalangaText);
        Assert.Equal(original.RegisterLabel, restored.RegisterLabel);
    }

    [Fact]
    public void Tenant_guard_rejects_mismatched_language()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TenantGuard.Ensure(LanguageId.New(), LanguageId.New()));
    }
}
