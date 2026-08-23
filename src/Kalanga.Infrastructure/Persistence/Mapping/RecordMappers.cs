using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Entities;

namespace Kalanga.Infrastructure.Persistence.Mapping;

internal static class RecordMappers
{
    public static User ToDomain(this UserRecord record) =>
        new(
            UserId.From(record.Id),
            LanguageId.From(record.LanguageId),
            record.Email,
            record.PasswordHash,
            record.DisplayName,
            Parse<Role>(record.Role),
            Parse<UserStatus>(record.Status),
            record.CreatedAt,
            record.UpdatedAt);

    public static UserRecord ToRecord(this User user) =>
        new()
        {
            Id = user.Id.Value,
            LanguageId = user.LanguageId.Value,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            DisplayName = user.DisplayName,
            Role = Store(user.Role),
            Status = Store(user.Status),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
        };

    public static void CopyTo(this User user, UserRecord record)
    {
        record.PasswordHash = user.PasswordHash;
        record.DisplayName = user.DisplayName;
        record.Role = Store(user.Role);
        record.Status = Store(user.Status);
        record.UpdatedAt = user.UpdatedAt;
    }

    public static Lesson ToDomain(this LessonRecord record) =>
        new(
            LessonId.From(record.Id),
            LanguageId.From(record.LanguageId),
            record.Title,
            Parse<Level>(record.Level),
            record.Category,
            record.IsScenario,
            record.ScenarioContext,
            Parse<LessonStatus>(record.Status),
            UserId.From(record.ContributorId),
            record.ReviewedBy is { } reviewer ? UserId.From(reviewer) : null,
            record.ReviewFeedback,
            record.XpReward,
            record.CreatedAt,
            record.UpdatedAt);

    public static LessonRecord ToRecord(this Lesson lesson) =>
        new()
        {
            Id = lesson.Id.Value,
            LanguageId = lesson.LanguageId.Value,
            Title = lesson.Title,
            Level = Store(lesson.Level),
            Category = lesson.Category,
            IsScenario = lesson.IsScenario,
            ScenarioContext = lesson.ScenarioContext,
            Status = Store(lesson.Status),
            ContributorId = lesson.ContributorId.Value,
            ReviewedBy = lesson.ReviewedBy?.Value,
            ReviewFeedback = lesson.ReviewFeedback,
            XpReward = lesson.XpReward,
            CreatedAt = lesson.CreatedAt,
            UpdatedAt = lesson.UpdatedAt,
        };

    public static void CopyTo(this Lesson lesson, LessonRecord record)
    {
        record.Title = lesson.Title;
        record.Level = Store(lesson.Level);
        record.Category = lesson.Category;
        record.IsScenario = lesson.IsScenario;
        record.ScenarioContext = lesson.ScenarioContext;
        record.Status = Store(lesson.Status);
        record.ReviewedBy = lesson.ReviewedBy?.Value;
        record.ReviewFeedback = lesson.ReviewFeedback;
        record.XpReward = lesson.XpReward;
        record.UpdatedAt = lesson.UpdatedAt;
    }

    public static Phrase ToDomain(this PhraseRecord record) =>
        new(
            PhraseId.From(record.Id),
            LanguageId.From(record.LanguageId),
            LessonId.From(record.LessonId),
            record.KalangaText,
            record.EnglishTranslation,
            record.SortOrder,
            record.CreatedAt);

    public static PhraseRecord ToRecord(this Phrase phrase) =>
        new()
        {
            Id = phrase.Id.Value,
            LanguageId = phrase.LanguageId.Value,
            LessonId = phrase.LessonId.Value,
            KalangaText = phrase.KalangaText,
            EnglishTranslation = phrase.EnglishTranslation,
            SortOrder = phrase.SortOrder,
            CreatedAt = phrase.CreatedAt,
        };

    public static void CopyTo(this Phrase phrase, PhraseRecord record)
    {
        record.KalangaText = phrase.KalangaText;
        record.EnglishTranslation = phrase.EnglishTranslation;
        record.SortOrder = phrase.SortOrder;
    }

    public static LanguageVariation ToDomain(this LanguageVariationRecord record) =>
        new(
            LanguageVariationId.From(record.Id),
            LanguageId.From(record.LanguageId),
            PhraseId.From(record.PhraseId),
            record.KalangaText,
            record.RegisterLabel,
            record.CreatedAt);

    public static LanguageVariationRecord ToRecord(this LanguageVariation variation) =>
        new()
        {
            Id = variation.Id.Value,
            LanguageId = variation.LanguageId.Value,
            PhraseId = variation.PhraseId.Value,
            KalangaText = variation.KalangaText,
            RegisterLabel = variation.RegisterLabel,
            CreatedAt = variation.CreatedAt,
        };

    public static void CopyTo(this LanguageVariation variation, LanguageVariationRecord record)
    {
        record.KalangaText = variation.KalangaText;
        record.RegisterLabel = variation.RegisterLabel;
    }

    public static AudioRecording ToDomain(this AudioRecordingRecord record) =>
        new(
            AudioRecordingId.From(record.Id),
            LanguageId.From(record.LanguageId),
            record.PhraseId is { } phraseId ? PhraseId.From(phraseId) : null,
            record.VariationId is { } variationId ? LanguageVariationId.From(variationId) : null,
            UserId.From(record.ContributorId),
            record.CdnUrl,
            Parse<AudioFileFormat>(record.FileFormat),
            record.FileSizeBytes,
            Parse<SpeakerGender>(record.SpeakerGender),
            record.DialectLabel,
            Parse<AudioRecordingStatus>(record.Status),
            record.DurationMs,
            record.CreatedAt);

    public static AudioRecordingRecord ToRecord(this AudioRecording recording) =>
        new()
        {
            Id = recording.Id.Value,
            LanguageId = recording.LanguageId.Value,
            PhraseId = recording.PhraseId?.Value,
            VariationId = recording.VariationId?.Value,
            ContributorId = recording.ContributorId.Value,
            CdnUrl = recording.CdnUrl,
            FileFormat = Store(recording.FileFormat),
            FileSizeBytes = recording.FileSizeBytes,
            SpeakerGender = Store(recording.SpeakerGender),
            DialectLabel = recording.DialectLabel,
            Status = Store(recording.Status),
            DurationMs = recording.DurationMs,
            CreatedAt = recording.CreatedAt,
        };

    public static void CopyTo(this AudioRecording recording, AudioRecordingRecord record)
    {
        record.Status = Store(recording.Status);
    }

    public static Exercise ToDomain(this ExerciseRecord record) =>
        new(
            ExerciseId.From(record.Id),
            LanguageId.From(record.LanguageId),
            LessonId.From(record.LessonId),
            Parse<ExerciseType>(record.ExerciseType),
            record.PromptData,
            record.CorrectAnswer,
            record.SortOrder,
            record.CreatedAt);

    public static ExerciseRecord ToRecord(this Exercise exercise) =>
        new()
        {
            Id = exercise.Id.Value,
            LanguageId = exercise.LanguageId.Value,
            LessonId = exercise.LessonId.Value,
            ExerciseType = Store(exercise.ExerciseType),
            PromptData = exercise.PromptData,
            CorrectAnswer = exercise.CorrectAnswer,
            SortOrder = exercise.SortOrder,
            CreatedAt = exercise.CreatedAt,
        };

    public static void CopyTo(this Exercise exercise, ExerciseRecord record)
    {
        record.ExerciseType = Store(exercise.ExerciseType);
        record.PromptData = exercise.PromptData;
        record.CorrectAnswer = exercise.CorrectAnswer;
        record.SortOrder = exercise.SortOrder;
    }

    public static ContentPack ToDomain(this ContentPackRecord record, IEnumerable<Guid> lessonIds) =>
        new(
            ContentPackId.From(record.Id),
            LanguageId.From(record.LanguageId),
            record.Name,
            record.Level is null ? null : Parse<Level>(record.Level),
            record.Category,
            record.Version,
            record.SizeBytes,
            record.ManifestUrl,
            lessonIds.Select(LessonId.From).ToHashSet(),
            record.CreatedAt,
            record.UpdatedAt);

    public static ContentPackRecord ToRecord(this ContentPack pack) =>
        new()
        {
            Id = pack.Id.Value,
            LanguageId = pack.LanguageId.Value,
            Name = pack.Name,
            Level = pack.Level is { } level ? Store(level) : null,
            Category = pack.Category,
            Version = pack.Version,
            SizeBytes = pack.SizeBytes,
            ManifestUrl = pack.ManifestUrl,
            CreatedAt = pack.CreatedAt,
            UpdatedAt = pack.UpdatedAt,
        };

    public static void CopyTo(this ContentPack pack, ContentPackRecord record)
    {
        record.Name = pack.Name;
        record.Level = pack.Level is { } level ? Store(level) : null;
        record.Category = pack.Category;
        record.Version = pack.Version;
        record.SizeBytes = pack.SizeBytes;
        record.ManifestUrl = pack.ManifestUrl;
        record.UpdatedAt = pack.UpdatedAt;
    }

    public static LearnerProgress ToDomain(this LearnerProgressRecord record) =>
        new(
            LearnerProgressId.From(record.Id),
            LanguageId.From(record.LanguageId),
            UserId.From(record.UserId),
            LessonId.From(record.LessonId),
            record.CompletedAt,
            record.Score,
            record.XpAwarded,
            record.UpdatedAt);

    public static LearnerProgressRecord ToRecord(this LearnerProgress progress) =>
        new()
        {
            Id = progress.Id.Value,
            LanguageId = progress.LanguageId.Value,
            UserId = progress.UserId.Value,
            LessonId = progress.LessonId.Value,
            CompletedAt = progress.CompletedAt,
            Score = progress.Score,
            XpAwarded = progress.XpAwarded,
            UpdatedAt = progress.UpdatedAt,
        };

    public static void CopyTo(this LearnerProgress progress, LearnerProgressRecord record)
    {
        record.CompletedAt = progress.CompletedAt;
        record.Score = progress.Score;
        record.XpAwarded = progress.XpAwarded;
        record.UpdatedAt = progress.UpdatedAt;
    }

    public static SpacedRepetitionRecord ToDomain(this SpacedRepetitionRecordRow record) =>
        new(
            SpacedRepetitionRecordId.From(record.Id),
            LanguageId.From(record.LanguageId),
            UserId.From(record.UserId),
            PhraseId.From(record.PhraseId),
            record.VariationId is { } variationId ? LanguageVariationId.From(variationId) : null,
            record.EaseFactor,
            record.IntervalDays,
            record.Repetitions,
            record.NextReviewAt,
            record.LastReviewedAt,
            record.UpdatedAt);

    public static SpacedRepetitionRecordRow ToRecord(this SpacedRepetitionRecord srs) =>
        new()
        {
            Id = srs.Id.Value,
            LanguageId = srs.LanguageId.Value,
            UserId = srs.UserId.Value,
            PhraseId = srs.PhraseId.Value,
            VariationId = srs.VariationId?.Value,
            EaseFactor = srs.EaseFactor,
            IntervalDays = srs.IntervalDays,
            Repetitions = srs.Repetitions,
            NextReviewAt = srs.NextReviewAt,
            LastReviewedAt = srs.LastReviewedAt,
            UpdatedAt = srs.UpdatedAt,
        };

    public static void CopyTo(this SpacedRepetitionRecord srs, SpacedRepetitionRecordRow record)
    {
        record.EaseFactor = srs.EaseFactor;
        record.IntervalDays = srs.IntervalDays;
        record.Repetitions = srs.Repetitions;
        record.NextReviewAt = srs.NextReviewAt;
        record.LastReviewedAt = srs.LastReviewedAt;
        record.UpdatedAt = srs.UpdatedAt;
    }

    public static LearnerGamification ToDomain(this LearnerGamificationRecord record) =>
        new(
            LearnerGamificationId.From(record.Id),
            LanguageId.From(record.LanguageId),
            UserId.From(record.UserId),
            record.TotalXp,
            record.CurrentStreak,
            record.LongestStreak,
            record.LastActivityDate,
            Parse<Level>(record.ProgressLevel),
            record.UpdatedAt);

    public static LearnerGamificationRecord ToRecord(this LearnerGamification gamification) =>
        new()
        {
            Id = gamification.Id.Value,
            LanguageId = gamification.LanguageId.Value,
            UserId = gamification.UserId.Value,
            TotalXp = gamification.TotalXp,
            CurrentStreak = gamification.CurrentStreak,
            LongestStreak = gamification.LongestStreak,
            LastActivityDate = gamification.LastActivityDate,
            ProgressLevel = Store(gamification.ProgressLevel),
            UpdatedAt = gamification.UpdatedAt,
        };

    public static void CopyTo(this LearnerGamification gamification, LearnerGamificationRecord record)
    {
        record.TotalXp = gamification.TotalXp;
        record.CurrentStreak = gamification.CurrentStreak;
        record.LongestStreak = gamification.LongestStreak;
        record.LastActivityDate = gamification.LastActivityDate;
        record.ProgressLevel = Store(gamification.ProgressLevel);
        record.UpdatedAt = gamification.UpdatedAt;
    }

    public static Request ToDomain(this RequestRecord record) =>
        new(
            RequestId.From(record.Id),
            LanguageId.From(record.LanguageId),
            UserId.From(record.SubmitterId),
            record.Title,
            record.Description,
            record.UpvoteCount,
            Parse<RequestStatus>(record.Status),
            record.FulfilledByLessonId is { } lessonId ? LessonId.From(lessonId) : null,
            record.CreatedAt,
            record.UpdatedAt);

    public static RequestRecord ToRecord(this Request request) =>
        new()
        {
            Id = request.Id.Value,
            LanguageId = request.LanguageId.Value,
            SubmitterId = request.SubmitterId.Value,
            Title = request.Title,
            Description = request.Description,
            UpvoteCount = request.UpvoteCount,
            Status = Store(request.Status),
            FulfilledByLessonId = request.FulfilledByLessonId?.Value,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
        };

    public static void CopyTo(this Request request, RequestRecord record)
    {
        record.UpvoteCount = request.UpvoteCount;
        record.Status = Store(request.Status);
        record.FulfilledByLessonId = request.FulfilledByLessonId?.Value;
        record.UpdatedAt = request.UpdatedAt;
    }

    public static SyncCheckpoint ToDomain(this SyncCheckpointRecord record) =>
        new(
            SyncCheckpointId.From(record.Id),
            LanguageId.From(record.LanguageId),
            UserId.From(record.UserId),
            record.LastSyncedAt,
            record.SyncVersion);

    public static SyncCheckpointRecord ToRecord(this SyncCheckpoint checkpoint) =>
        new()
        {
            Id = checkpoint.Id.Value,
            LanguageId = checkpoint.LanguageId.Value,
            UserId = checkpoint.UserId.Value,
            LastSyncedAt = checkpoint.LastSyncedAt,
            SyncVersion = checkpoint.SyncVersion,
        };

    public static void CopyTo(this SyncCheckpoint checkpoint, SyncCheckpointRecord record)
    {
        record.LastSyncedAt = checkpoint.LastSyncedAt;
        record.SyncVersion = checkpoint.SyncVersion;
    }

    public static SyncPushReceipt ToDomain(this SyncPushReceiptRecord record) =>
        new(
            SyncPushReceiptId.From(record.Id),
            LanguageId.From(record.LanguageId),
            UserId.From(record.UserId),
            record.ClientOperationId,
            record.CreatedAt);

    public static SyncPushReceiptRecord ToRecord(this SyncPushReceipt receipt) =>
        new()
        {
            Id = receipt.Id.Value,
            LanguageId = receipt.LanguageId.Value,
            UserId = receipt.UserId.Value,
            ClientOperationId = receipt.ClientOperationId,
            CreatedAt = receipt.CreatedAt,
        };

    private static string Store<TEnum>(TEnum value) where TEnum : struct, Enum => value.ToString();

    private static TEnum Parse<TEnum>(string value) where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(value, ignoreCase: true);
}
