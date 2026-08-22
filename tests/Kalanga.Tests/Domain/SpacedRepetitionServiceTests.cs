using Kalanga.Domain;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Services;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Tests.Domain;

public sealed class SpacedRepetitionServiceTests
{
    private readonly SpacedRepetitionService _srs = new();

    [Fact]
    public void Correct_answer_lengthens_the_interval_after_the_first_success()
    {
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var record = SpacedRepetitionRecord.Create(LanguageId.New(), UserId.New(), PhraseId.New(), now);

        _srs.RecordAnswer(record, isCorrect: true, now);
        Assert.Equal(1, record.IntervalDays);
        Assert.Equal(1, record.Repetitions);
        Assert.Equal(new DateOnly(2026, 8, 23), record.NextReviewAt);
        Assert.Equal(2.60m, record.EaseFactor);

        _srs.RecordAnswer(record, isCorrect: true, now);
        Assert.Equal(DomainRules.SrsSecondIntervalDays, record.IntervalDays);
        Assert.Equal(2, record.Repetitions);
        Assert.Equal(new DateOnly(2026, 8, 28), record.NextReviewAt);

        var intervalAfterSecond = record.IntervalDays;
        _srs.RecordAnswer(record, isCorrect: true, now);
        Assert.True(record.IntervalDays > intervalAfterSecond);
        Assert.Equal(3, record.Repetitions);
        Assert.Equal(now, record.LastReviewedAt);
    }

    [Fact]
    public void Incorrect_answer_shortens_the_interval_and_schedules_sooner()
    {
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var record = SpacedRepetitionRecord.Create(LanguageId.New(), UserId.New(), PhraseId.New(), now);
        _srs.RecordAnswer(record, isCorrect: true, now);
        _srs.RecordAnswer(record, isCorrect: true, now);

        var previousInterval = record.IntervalDays;
        var previousNext = record.NextReviewAt;
        var previousEase = record.EaseFactor;

        _srs.RecordAnswer(record, isCorrect: false, now);

        Assert.Equal(0, record.Repetitions);
        Assert.Equal(DomainRules.DefaultSrsIntervalDays, record.IntervalDays);
        Assert.True(record.IntervalDays < previousInterval);
        Assert.True(record.NextReviewAt < previousNext);
        Assert.True(record.EaseFactor < previousEase);
        Assert.Equal(new DateOnly(2026, 8, 23), record.NextReviewAt);
    }

    [Fact]
    public void Phrase_and_variation_records_are_distinct_identities()
    {
        var now = DateTimeOffset.UtcNow;
        var phraseId = PhraseId.New();
        var baseRecord = SpacedRepetitionRecord.Create(LanguageId.New(), UserId.New(), phraseId, now);
        var variationRecord = SpacedRepetitionRecord.Create(
            baseRecord.LanguageId,
            baseRecord.UserId,
            phraseId,
            now,
            LanguageVariationId.New());

        _srs.RecordAnswer(baseRecord, isCorrect: true, now);
        _srs.RecordAnswer(baseRecord, isCorrect: true, now);
        _srs.RecordAnswer(variationRecord, isCorrect: false, now);

        Assert.Null(baseRecord.VariationId);
        Assert.NotNull(variationRecord.VariationId);
        Assert.Equal(6, baseRecord.IntervalDays);
        Assert.Equal(1, variationRecord.IntervalDays);
        Assert.NotEqual(baseRecord.Id, variationRecord.Id);
    }
}
