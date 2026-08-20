using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class Request
{
    internal Request(
        RequestId id,
        LanguageId languageId,
        UserId submitterId,
        string title,
        string description,
        int upvoteCount,
        RequestStatus status,
        LessonId? fulfilledByLessonId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        LanguageId = languageId;
        SubmitterId = submitterId;
        Title = title;
        Description = description;
        UpvoteCount = upvoteCount;
        Status = status;
        FulfilledByLessonId = fulfilledByLessonId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public RequestId Id { get; }

    public LanguageId LanguageId { get; }

    public UserId SubmitterId { get; }

    public string Title { get; }

    public string Description { get; }

    public int UpvoteCount { get; private set; }

    public RequestStatus Status { get; private set; }

    public LessonId? FulfilledByLessonId { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Request Submit(
        LanguageId languageId,
        UserId submitterId,
        string title,
        string description,
        DateTimeOffset utcNow)
    {
        return new Request(
            RequestId.New(),
            languageId,
            submitterId,
            Guard.Required(title, nameof(title), 255),
            Guard.RequiredText(description, nameof(description)),
            upvoteCount: 0,
            RequestStatus.Open,
            fulfilledByLessonId: null,
            utcNow,
            utcNow);
    }

    public void AddUpvote(DateTimeOffset utcNow)
    {
        EnsureOpen();
        UpvoteCount++;
        UpdatedAt = utcNow;
    }

    public void Fulfill(LessonId lessonId, DateTimeOffset utcNow)
    {
        EnsureOpen();
        Status = RequestStatus.Fulfilled;
        FulfilledByLessonId = lessonId;
        UpdatedAt = utcNow;
    }

    private void EnsureOpen()
    {
        if (Status != RequestStatus.Open)
        {
            throw new InvalidRequestStateException("A fulfilled request cannot be modified.");
        }
    }
}
