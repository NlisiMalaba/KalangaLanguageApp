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
