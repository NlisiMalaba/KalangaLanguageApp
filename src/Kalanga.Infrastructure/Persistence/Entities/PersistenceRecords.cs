namespace Kalanga.Infrastructure.Persistence.Entities;

public sealed class LanguageRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class UserRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class RefreshTokenRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class LessonRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsScenario { get; set; }
    public string? ScenarioContext { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid ContributorId { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewFeedback { get; set; }
    public int XpReward { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PhraseRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public Guid LessonId { get; set; }
    public string KalangaText { get; set; } = string.Empty;
    public string EnglishTranslation { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class LanguageVariationRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public Guid PhraseId { get; set; }
    public string KalangaText { get; set; } = string.Empty;
    public string RegisterLabel { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AudioRecordingRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public Guid? PhraseId { get; set; }
    public Guid? VariationId { get; set; }
    public Guid ContributorId { get; set; }
    public string CdnUrl { get; set; } = string.Empty;
    public string FileFormat { get; set; } = string.Empty;
    public int FileSizeBytes { get; set; }
    public string SpeakerGender { get; set; } = string.Empty;
    public string? DialectLabel { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ExerciseRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public Guid LessonId { get; set; }
    public string ExerciseType { get; set; } = string.Empty;
    public string PromptData { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ContentPackRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Level { get; set; }
    public string? Category { get; set; }
    public int Version { get; set; }
    public long SizeBytes { get; set; }
    public string ManifestUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ContentPackLessonRecord
{
    public Guid PackId { get; set; }
    public Guid LessonId { get; set; }
}

public sealed class LearnerProgressRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int? Score { get; set; }
    public int XpAwarded { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ExerciseResultRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public Guid UserId { get; set; }
    public Guid ExerciseId { get; set; }
    public bool IsCorrect { get; set; }
    public DateTimeOffset AnsweredAt { get; set; }
}

public sealed class SpacedRepetitionRecordRow
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public Guid UserId { get; set; }
    public Guid PhraseId { get; set; }
    public Guid? VariationId { get; set; }
    public decimal EaseFactor { get; set; }
    public int IntervalDays { get; set; }
    public int Repetitions { get; set; }
    public DateOnly NextReviewAt { get; set; }
    public DateTimeOffset? LastReviewedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class LearnerGamificationRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public Guid UserId { get; set; }
    public int TotalXp { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateOnly? LastActivityDate { get; set; }
    public string ProgressLevel { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class RequestRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public Guid SubmitterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int UpvoteCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? FulfilledByLessonId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class RequestUpvoteRecord
{
    public Guid RequestId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class SyncCheckpointRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LanguageId { get; set; }
    public DateTimeOffset LastSyncedAt { get; set; }
    public long SyncVersion { get; set; }
}

public sealed class NotificationOutboxRecord
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public Guid RecipientUserId { get; set; }
    public Guid LessonId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
