using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class LearnerGamification
{
    internal LearnerGamification(
        LearnerGamificationId id,
        LanguageId languageId,
        UserId userId,
        int totalXp,
        int currentStreak,
        int longestStreak,
        DateOnly? lastActivityDate,
        Level progressLevel,
        DateTimeOffset updatedAt)
    {
        Id = id;
        LanguageId = languageId;
        UserId = userId;
        TotalXp = totalXp;
        CurrentStreak = currentStreak;
        LongestStreak = longestStreak;
        LastActivityDate = lastActivityDate;
        ProgressLevel = progressLevel;
        UpdatedAt = updatedAt;
    }

    public LearnerGamificationId Id { get; }

    public LanguageId LanguageId { get; }

    public UserId UserId { get; }

    public int TotalXp { get; private set; }

    public int CurrentStreak { get; private set; }

    public int LongestStreak { get; private set; }

    public DateOnly? LastActivityDate { get; private set; }

    public Level ProgressLevel { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static LearnerGamification Create(LanguageId languageId, UserId userId, DateTimeOffset utcNow)
    {
        return new LearnerGamification(
            LearnerGamificationId.New(),
            languageId,
            userId,
            totalXp: 0,
            currentStreak: 0,
            longestStreak: 0,
            lastActivityDate: null,
            Level.Beginner,
            utcNow);
    }

    public void AwardXp(int xp, DateTimeOffset utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(xp);
        TotalXp += xp;
        UpdatedAt = utcNow;
    }

    public bool AdvanceLevelIfThresholdCrossed(DateTimeOffset utcNow)
    {
        var next = ResolveLevel(TotalXp);
        if (next == ProgressLevel)
        {
            return false;
        }

        ProgressLevel = next;
        UpdatedAt = utcNow;
        return true;
    }

    public void RecordActivity(DateOnly activityDate, DateTimeOffset utcNow)
    {
        if (LastActivityDate == activityDate)
        {
            UpdatedAt = utcNow;
            return;
        }

        if (LastActivityDate is { } last && last.AddDays(1) == activityDate)
        {
            CurrentStreak++;
        }
        else
        {
            CurrentStreak = 1;
        }

        if (CurrentStreak > LongestStreak)
        {
            LongestStreak = CurrentStreak;
        }

        LastActivityDate = activityDate;
        UpdatedAt = utcNow;
    }

    public void ResetStreakIfMissed(DateOnly asOfDate, DateTimeOffset utcNow)
    {
        if (LastActivityDate is not { } last || last.AddDays(1) >= asOfDate)
        {
            return;
        }

        ResetStreak(utcNow);
    }

    public void ResetStreak(DateTimeOffset utcNow)
    {
        CurrentStreak = 0;
        UpdatedAt = utcNow;
    }

    public void MergeAdditive(
        int additionalXp,
        int incomingCurrentStreak,
        int incomingLongestStreak,
        DateOnly? incomingLastActivityDate,
        DateTimeOffset utcNow)
    {
        Guard.NonNegative(additionalXp, nameof(additionalXp));
        Guard.NonNegative(incomingCurrentStreak, nameof(incomingCurrentStreak));
        Guard.NonNegative(incomingLongestStreak, nameof(incomingLongestStreak));

        if (additionalXp > 0)
        {
            AwardXp(additionalXp, utcNow);
        }

        CurrentStreak = Math.Max(CurrentStreak, incomingCurrentStreak);
        LongestStreak = Math.Max(LongestStreak, Math.Max(incomingLongestStreak, CurrentStreak));
        if (incomingLastActivityDate is { } incoming
            && (LastActivityDate is null || incoming > LastActivityDate.Value))
        {
            LastActivityDate = incoming;
        }

        AdvanceLevelIfThresholdCrossed(utcNow);
        UpdatedAt = utcNow;
    }

    private static Level ResolveLevel(int totalXp)
    {
        if (totalXp >= DomainRules.AdvancedXpThreshold)
        {
            return Level.Advanced;
        }

        if (totalXp >= DomainRules.IntermediateXpThreshold)
        {
            return Level.Intermediate;
        }

        return Level.Beginner;
    }
}
