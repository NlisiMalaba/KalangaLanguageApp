namespace Kalanga.Domain.Enums;

public enum Role
{
    Learner = 0,
    Contributor = 1,
    Reviewer = 2,
    Admin = 3,
}

public enum UserStatus
{
    Active = 0,
    Suspended = 1,
}

public enum Level
{
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2,
}

public enum LessonStatus
{
    Draft = 0,
    PendingReview = 1,
    Published = 2,
    Unpublished = 3,
}

public enum ExerciseType
{
    Flashcard = 0,
    MultipleChoice = 1,
    SentenceBuilder = 2,
    Listening = 3,
}

public enum AudioFileFormat
{
    Mp3 = 0,
    Aac = 1,
}

public enum SpeakerGender
{
    Unspecified = 0,
    Male = 1,
    Female = 2,
}

public enum AudioRecordingStatus
{
    PendingReview = 0,
    Approved = 1,
    Rejected = 2,
}

public enum RequestStatus
{
    Open = 0,
    Fulfilled = 1,
}
