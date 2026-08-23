using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class SpacedRepetitionRecord
{
    internal SpacedRepetitionRecord(
        SpacedRepetitionRecordId id,
        LanguageId languageId,
        UserId userId,
        PhraseId phraseId,
        LanguageVariationId? variationId,
        decimal easeFactor,
        int intervalDays,
        int repetitions,
        DateOnly nextReviewAt,
        DateTimeOffset? lastReviewedAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        LanguageId = languageId;
        UserId = userId;
        PhraseId = phraseId;
        VariationId = variationId;
        EaseFactor = easeFactor;
        IntervalDays = intervalDays;
        Repetitions = repetitions;
        NextReviewAt = nextReviewAt;
        LastReviewedAt = lastReviewedAt;
        UpdatedAt = updatedAt;
    }

    public SpacedRepetitionRecordId Id { get; }

    public LanguageId LanguageId { get; }

    public UserId UserId { get; }

    public PhraseId PhraseId { get; }

    public LanguageVariationId? VariationId { get; }

    public decimal EaseFactor { get; private set; }

    public int IntervalDays { get; private set; }

    public int Repetitions { get; private set; }

    public DateOnly NextReviewAt { get; private set; }

    public DateTimeOffset? LastReviewedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static SpacedRepetitionRecord Create(
        LanguageId languageId,
        UserId userId,
        PhraseId phraseId,
        DateTimeOffset utcNow,
        LanguageVariationId? variationId = null)
    {
        var today = DateOnly.FromDateTime(utcNow.UtcDateTime);
        return new SpacedRepetitionRecord(
            SpacedRepetitionRecordId.New(),
            languageId,
            userId,
            phraseId,
            variationId,
            DomainRules.DefaultEaseFactor,
            DomainRules.DefaultSrsIntervalDays,
            repetitions: 0,
            nextReviewAt: today,
            lastReviewedAt: null,
            utcNow);
    }

    public void Schedule(decimal easeFactor, int intervalDays, int repetitions, DateOnly nextReviewAt, DateTimeOffset utcNow)
    {
        ApplySchedule(easeFactor, intervalDays, repetitions, nextReviewAt, utcNow, utcNow);
    }

    public static SpacedRepetitionRecord FromClient(
        LanguageId languageId,
        UserId userId,
        PhraseId phraseId,
        LanguageVariationId? variationId,
        decimal easeFactor,
        int intervalDays,
        int repetitions,
        DateOnly nextReviewAt,
        DateTimeOffset? lastReviewedAt,
        DateTimeOffset updatedAt)
    {
        var record = Create(languageId, userId, phraseId, updatedAt, variationId);
        record.ApplySchedule(easeFactor, intervalDays, repetitions, nextReviewAt, lastReviewedAt, updatedAt);
        return record;
    }

    public bool ApplyLastWriteWins(
        decimal easeFactor,
        int intervalDays,
        int repetitions,
        DateOnly nextReviewAt,
        DateTimeOffset? lastReviewedAt,
        DateTimeOffset incomingUpdatedAt)
    {
        if (incomingUpdatedAt <= UpdatedAt)
        {
            return false;
        }

        ApplySchedule(easeFactor, intervalDays, repetitions, nextReviewAt, lastReviewedAt, incomingUpdatedAt);
        return true;
    }

    private void ApplySchedule(
        decimal easeFactor,
        int intervalDays,
        int repetitions,
        DateOnly nextReviewAt,
        DateTimeOffset? lastReviewedAt,
        DateTimeOffset updatedAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(easeFactor, 0m);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(intervalDays);
        Guard.NonNegative(repetitions, nameof(repetitions));

        EaseFactor = easeFactor;
        IntervalDays = intervalDays;
        Repetitions = repetitions;
        NextReviewAt = nextReviewAt;
        LastReviewedAt = lastReviewedAt;
        UpdatedAt = updatedAt;
    }
}
