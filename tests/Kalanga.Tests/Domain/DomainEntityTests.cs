using Kalanga.Domain;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Tests.Domain;

public sealed class UserTests
{
    [Fact]
    public void Register_assigns_learner_role_and_active_status()
    {
        var now = DateTimeOffset.UtcNow;

        var user = User.Register(LanguageId.New(), "Learner@example.com", "hash", "Nomsa", now);

        Assert.Equal(Role.Learner, user.Role);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal("learner@example.com", user.Email);
        Assert.False(user.IsSuspended);
    }
}

public sealed class LessonTests
{
    private static Lesson Draft(DateTimeOffset now) =>
        Lesson.CreateDraft(LanguageId.New(), UserId.New(), "Greetings", Level.Beginner, "Everyday", now);

    [Fact]
    public void Submit_then_approve_publishes_lesson()
    {
        var now = DateTimeOffset.UtcNow;
        var lesson = Draft(now);
        var reviewer = UserId.New();

        lesson.SubmitForReview(now);
        Assert.Equal(LessonStatus.PendingReview, lesson.Status);
        Assert.False(lesson.IsVisibleInCatalog);

        lesson.Approve(reviewer, now);
        Assert.Equal(LessonStatus.Published, lesson.Status);
        Assert.True(lesson.IsVisibleInCatalog);
        Assert.Equal(reviewer, lesson.ReviewedBy);
    }

    [Fact]
    public void Request_revision_returns_lesson_to_draft()
    {
        var now = DateTimeOffset.UtcNow;
        var lesson = Draft(now);
        lesson.SubmitForReview(now);

        lesson.RequestRevision(UserId.New(), "Fix the translation.", now);

        Assert.Equal(LessonStatus.Draft, lesson.Status);
        Assert.False(lesson.IsVisibleInCatalog);
        Assert.Equal("Fix the translation.", lesson.ReviewFeedback);
    }

    [Fact]
    public void Reject_unpublishes_and_cannot_be_approved_from_that_state()
    {
        var now = DateTimeOffset.UtcNow;
        var lesson = Draft(now);
        lesson.SubmitForReview(now);

        lesson.Reject(UserId.New(), "Inaccurate audio.", now);

        Assert.Equal(LessonStatus.Unpublished, lesson.Status);
        Assert.Throws<InvalidLessonStatusTransitionException>(() => lesson.Approve(UserId.New(), now));
    }

    [Fact]
    public void Admin_can_publish_or_unpublish_from_any_status()
    {
        var now = DateTimeOffset.UtcNow;
        var lesson = Draft(now);

        lesson.Publish(now);
        Assert.Equal(LessonStatus.Published, lesson.Status);

        lesson.Unpublish(now);
        Assert.Equal(LessonStatus.Unpublished, lesson.Status);
    }
}

public sealed class RequestTests
{
    [Fact]
    public void Fulfill_sets_lesson_and_prevents_reopening()
    {
        var now = DateTimeOffset.UtcNow;
        var request = Request.Submit(LanguageId.New(), UserId.New(), "Market phrases", "Need bargaining vocabulary.", now);
        var lessonId = LessonId.New();

        request.Fulfill(lessonId, now);

        Assert.Equal(RequestStatus.Fulfilled, request.Status);
        Assert.Equal(lessonId, request.FulfilledByLessonId);
        Assert.Throws<InvalidRequestStateException>(() => request.AddUpvote(now));
    }
}

public sealed class AudioRecordingTests
{
    [Fact]
    public void New_recording_starts_pending_review_and_is_not_playable()
    {
        var recording = AudioRecording.Create(
            LanguageId.New(),
            UserId.New(),
            "https://cdn.example/audio.mp3",
            AudioFileFormat.Mp3,
            fileSizeBytes: 1024,
            durationMs: 1500,
            DateTimeOffset.UtcNow,
            phraseId: PhraseId.New());

        Assert.Equal(AudioRecordingStatus.PendingReview, recording.Status);
        Assert.False(recording.IsPlayableByLearners);
    }

    [Fact]
    public void Oversized_file_is_rejected()
    {
        Assert.Throws<InvalidAudioRecordingException>(() =>
            AudioRecording.Create(
                LanguageId.New(),
                UserId.New(),
                "https://cdn.example/audio.mp3",
                AudioFileFormat.Mp3,
                fileSizeBytes: DomainRules.MaxAudioFileSizeBytes + 1,
                durationMs: 1500,
                DateTimeOffset.UtcNow,
                phraseId: PhraseId.New()));
    }
}

public sealed class LearnerGamificationTests
{
    [Fact]
    public void Consecutive_days_increment_streak_and_xp_advances_level()
    {
        var now = DateTimeOffset.UtcNow;
        var gamification = LearnerGamification.Create(LanguageId.New(), UserId.New(), now);
        var day = new DateOnly(2026, 8, 1);

        gamification.RecordActivity(day, now);
        gamification.RecordActivity(day.AddDays(1), now);
        gamification.AwardXp(DomainRules.IntermediateXpThreshold, now);
        gamification.AdvanceLevelIfThresholdCrossed(now);

        Assert.Equal(2, gamification.CurrentStreak);
        Assert.Equal(Level.Intermediate, gamification.ProgressLevel);
    }

    [Fact]
    public void Missed_day_resets_streak_without_recording_activity()
    {
        var now = DateTimeOffset.UtcNow;
        var gamification = LearnerGamification.Create(LanguageId.New(), UserId.New(), now);
        var day = new DateOnly(2026, 8, 1);

        gamification.RecordActivity(day, now);
        gamification.ResetStreakIfMissed(day.AddDays(2), now);

        Assert.Equal(0, gamification.CurrentStreak);
        Assert.Equal(day, gamification.LastActivityDate);
    }

    [Fact]
    public void Additive_merge_adds_xp_and_keeps_the_higher_streak()
    {
        var now = DateTimeOffset.UtcNow;
        var gamification = LearnerGamification.Create(LanguageId.New(), UserId.New(), now);
        gamification.AwardXp(10, now);
        gamification.RecordActivity(new DateOnly(2026, 8, 1), now);

        gamification.MergeAdditive(
            additionalXp: 7,
            incomingCurrentStreak: 4,
            incomingLongestStreak: 9,
            incomingLastActivityDate: new DateOnly(2026, 8, 10),
            now);

        Assert.Equal(17, gamification.TotalXp);
        Assert.Equal(4, gamification.CurrentStreak);
        Assert.Equal(9, gamification.LongestStreak);
        Assert.Equal(new DateOnly(2026, 8, 10), gamification.LastActivityDate);
    }
}

public sealed class LearnerProgressTests
{
    [Fact]
    public void Last_write_wins_keeps_the_newer_client_payload()
    {
        var now = DateTimeOffset.UtcNow;
        var progress = LearnerProgress.Start(LanguageId.New(), UserId.New(), LessonId.New(), now);
        progress.Complete(40, 10, now);

        Assert.False(progress.ApplyLastWriteWins(now.AddMinutes(-1), 99, 10, now.AddMinutes(-1)));
        Assert.Equal(40, progress.Score);

        Assert.True(progress.ApplyLastWriteWins(now.AddMinutes(1), 88, 10, now.AddMinutes(1)));
        Assert.Equal(88, progress.Score);
        Assert.Equal(now.AddMinutes(1), progress.UpdatedAt);
    }
}

public sealed class IdTests
{
    [Fact]
    public void Empty_guid_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => LanguageId.From(Guid.Empty));
        Assert.Throws<ArgumentException>(() => UserId.From(Guid.Empty));
        Assert.Throws<ArgumentException>(() => LessonId.From(Guid.Empty));
        Assert.Throws<ArgumentException>(() => PhraseId.From(Guid.Empty));
    }
}
