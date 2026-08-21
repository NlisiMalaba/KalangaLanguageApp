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
