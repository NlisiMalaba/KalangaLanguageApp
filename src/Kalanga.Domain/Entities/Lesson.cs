using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class Lesson
{
    private Lesson(
        LessonId id,
        LanguageId languageId,
        string title,
        Level level,
        string category,
        bool isScenario,
        string? scenarioContext,
        LessonStatus status,
        UserId contributorId,
        UserId? reviewedBy,
        string? reviewFeedback,
        int xpReward,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        LanguageId = languageId;
        Title = title;
        Level = level;
        Category = category;
        IsScenario = isScenario;
        ScenarioContext = scenarioContext;
        Status = status;
        ContributorId = contributorId;
        ReviewedBy = reviewedBy;
        ReviewFeedback = reviewFeedback;
        XpReward = xpReward;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public LessonId Id { get; }

    public LanguageId LanguageId { get; }

    public string Title { get; private set; }

    public Level Level { get; private set; }

    public string Category { get; private set; }

    public bool IsScenario { get; private set; }

    public string? ScenarioContext { get; private set; }

    public LessonStatus Status { get; private set; }

    public UserId ContributorId { get; }

    public UserId? ReviewedBy { get; private set; }

    public string? ReviewFeedback { get; private set; }

    public int XpReward { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsVisibleInCatalog => Status == LessonStatus.Published;

    public static Lesson CreateDraft(
        LanguageId languageId,
        UserId contributorId,
        string title,
        Level level,
        string category,
        DateTimeOffset utcNow,
        bool isScenario = false,
        string? scenarioContext = null,
        int xpReward = DomainRules.DefaultLessonXpReward)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(xpReward);

        return new Lesson(
            LessonId.New(),
            languageId,
            Guard.Required(title, nameof(title), 255),
            level,
            Guard.Required(category, nameof(category), 100),
            isScenario,
            string.IsNullOrWhiteSpace(scenarioContext) ? null : scenarioContext.Trim(),
            LessonStatus.Draft,
            contributorId,
            reviewedBy: null,
            reviewFeedback: null,
            xpReward,
            utcNow,
            utcNow);
    }

    public void UpdateDraft(
        string title,
        Level level,
        string category,
        bool isScenario,
        string? scenarioContext,
        int xpReward,
        DateTimeOffset utcNow)
    {
        EnsureDraft("update");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(xpReward);

        Title = Guard.Required(title, nameof(title), 255);
        Level = level;
        Category = Guard.Required(category, nameof(category), 100);
        IsScenario = isScenario;
        ScenarioContext = string.IsNullOrWhiteSpace(scenarioContext) ? null : scenarioContext.Trim();
        XpReward = xpReward;
        Touch(utcNow);
    }

    public void SubmitForReview(DateTimeOffset utcNow)
    {
        EnsureStatus(LessonStatus.Draft, "submit for review");
        Status = LessonStatus.PendingReview;
        ReviewFeedback = null;
        Touch(utcNow);
    }

    public void Approve(UserId reviewerId, DateTimeOffset utcNow)
    {
        EnsureStatus(LessonStatus.PendingReview, "approve");
        Status = LessonStatus.Published;
        ReviewedBy = reviewerId;
        ReviewFeedback = null;
        Touch(utcNow);
    }

    public void Reject(UserId reviewerId, string feedback, DateTimeOffset utcNow)
    {
        EnsureStatus(LessonStatus.PendingReview, "reject");
        Status = LessonStatus.Unpublished;
        ReviewedBy = reviewerId;
        ReviewFeedback = Guard.RequiredText(feedback, nameof(feedback));
        Touch(utcNow);
    }

    public void RequestRevision(UserId reviewerId, string feedback, DateTimeOffset utcNow)
    {
        EnsureStatus(LessonStatus.PendingReview, "request revision");
        Status = LessonStatus.Draft;
        ReviewedBy = reviewerId;
        ReviewFeedback = Guard.RequiredText(feedback, nameof(feedback));
        Touch(utcNow);
    }

    public void Publish(DateTimeOffset utcNow)
    {
        Status = LessonStatus.Published;
        Touch(utcNow);
    }

    public void Unpublish(DateTimeOffset utcNow)
    {
        Status = LessonStatus.Unpublished;
        Touch(utcNow);
    }

    private void EnsureDraft(string action) => EnsureStatus(LessonStatus.Draft, action);

    private void EnsureStatus(LessonStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidLessonStatusTransitionException(Status, action);
        }
    }

    private void Touch(DateTimeOffset utcNow) => UpdatedAt = utcNow;
}
