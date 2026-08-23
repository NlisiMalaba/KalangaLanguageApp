namespace Kalanga.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }
}

public sealed class InvalidLessonStatusTransitionException : DomainException
{
    public InvalidLessonStatusTransitionException(Enums.LessonStatus from, string action)
        : base($"Cannot {action} a lesson in {from} status.")
    {
        From = from;
        Action = action;
    }

    public Enums.LessonStatus From { get; }

    public string Action { get; }
}

public sealed class InvalidRequestStateException : DomainException
{
    public InvalidRequestStateException(string message)
        : base(message)
    {
    }
}

public sealed class InvalidAudioRecordingException : DomainException
{
    public InvalidAudioRecordingException(string message)
        : base(message)
    {
    }
}

public sealed class DuplicateEmailException : DomainException
{
    public DuplicateEmailException(string email)
        : base("An account with this email address already exists.")
    {
        Email = email;
    }

    public string Email { get; }
}

public sealed class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}

public sealed class UserSuspendedException : DomainException
{
    public UserSuspendedException()
        : base("This account has been suspended.")
    {
    }
}

public sealed class UnauthorizedRoleException : DomainException
{
    public UnauthorizedRoleException(string action, Enums.Role requiredRole)
        : base($"Role '{requiredRole}' is required to {action}.")
    {
        Action = action;
        RequiredRole = requiredRole;
    }

    public string Action { get; }

    public Enums.Role RequiredRole { get; }
}

public sealed class UserNotFoundException : DomainException
{
    public UserNotFoundException(ValueObjects.UserId userId)
        : base($"User '{userId}' was not found.")
    {
        UserId = userId;
    }

    public ValueObjects.UserId UserId { get; }
}

public sealed class LessonNotFoundException : DomainException
{
    public LessonNotFoundException(ValueObjects.LessonId lessonId)
        : base($"Lesson '{lessonId}' was not found.")
    {
        LessonId = lessonId;
    }

    public ValueObjects.LessonId LessonId { get; }
}

public sealed class RequestNotFoundException : DomainException
{
    public RequestNotFoundException(ValueObjects.RequestId requestId)
        : base($"Request '{requestId}' was not found.")
    {
        RequestId = requestId;
    }

    public ValueObjects.RequestId RequestId { get; }
}

public sealed class LessonAccessDeniedException : DomainException
{
    public LessonAccessDeniedException(string action)
        : base($"You are not allowed to {action} this lesson.")
    {
        Action = action;
    }

    public string Action { get; }
}

public sealed class InvalidUserManagementException : DomainException
{
    public InvalidUserManagementException(string message)
        : base(message)
    {
    }
}

public sealed class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException()
        : base("Refresh token is invalid or expired.")
    {
    }
}

public sealed class InvalidReviewException : DomainException
{
    public InvalidReviewException(string message)
        : base(message)
    {
    }
}

public sealed class InvalidAudioUploadException : DomainException
{
    public InvalidAudioUploadException(string message)
        : base(message)
    {
    }
}

public sealed class PhraseNotFoundException : DomainException
{
    public PhraseNotFoundException(ValueObjects.PhraseId phraseId)
        : base($"Phrase '{phraseId}' was not found.")
    {
        PhraseId = phraseId;
    }

    public ValueObjects.PhraseId PhraseId { get; }
}

public sealed class VariationNotFoundException : DomainException
{
    public VariationNotFoundException(ValueObjects.LanguageVariationId variationId)
        : base($"Language variation '{variationId}' was not found.")
    {
        VariationId = variationId;
    }

    public ValueObjects.LanguageVariationId VariationId { get; }
}

public sealed class AudioStorageUnavailableException : DomainException
{
    public AudioStorageUnavailableException(string message)
        : base(message)
    {
    }
}

public sealed class AudioRecordingNotFoundException : DomainException
{
    public AudioRecordingNotFoundException(ValueObjects.AudioRecordingId audioRecordingId)
        : base($"Audio recording '{audioRecordingId}' was not found.")
    {
        AudioRecordingId = audioRecordingId;
    }

    public ValueObjects.AudioRecordingId AudioRecordingId { get; }
}

public sealed class ContentPackNotFoundException : DomainException
{
    public ContentPackNotFoundException(ValueObjects.ContentPackId packId)
        : base($"Content pack '{packId}' was not found.")
    {
        PackId = packId;
    }

    public ValueObjects.ContentPackId PackId { get; }
}

public sealed class TenantAccessDeniedException : DomainException
{
    public TenantAccessDeniedException()
        : base("The requested language does not match this account.")
    {
    }
}

public sealed class InvalidLessonCompletionException : DomainException
{
    public InvalidLessonCompletionException(string message)
        : base(message)
    {
    }
}

public sealed class DuplicateLearnerProgressException : DomainException
{
    public DuplicateLearnerProgressException()
        : base("Progress for this lesson has already been recorded.")
    {
    }
}

public sealed class InvalidSyncPayloadException : DomainException
{
    public InvalidSyncPayloadException(string message)
        : base(message)
    {
    }
}

public sealed class DuplicateSyncPushException : DomainException
{
    public DuplicateSyncPushException(string clientOperationId)
        : base("This sync payload has already been applied.")
    {
        ClientOperationId = clientOperationId;
    }

    public string ClientOperationId { get; }
}
