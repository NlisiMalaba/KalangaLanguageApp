using CsCheck;
using Kalanga.Domain;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Services;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Tests.Domain;

public sealed class IncorrectAnswerDecreasesSrsIntervalPropertyTests
{
    // Feature: kalanga-language-app, Property 14: Incorrect Answer Decreases SRS Interval
    [Fact]
    public void Incorrect_answer_shortens_interval_and_schedules_the_next_review_sooner()
    {
        var input =
            from easeCents in Gen.Int[130, 350]
            from interval in Gen.Int[2, 40]
            from repetitions in Gen.Int[1, 8]
            select (easeCents, interval, repetitions);

        var srs = new SpacedRepetitionService();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        Check.Sample(
            input,
            (int easeCents, int interval, int repetitions) =>
            {
                var ease = easeCents / 100m;
                var previousNext = today.AddDays(interval);
                var record = SpacedRepetitionRecord.Create(LanguageId.New(), UserId.New(), PhraseId.New(), now);
                record.Schedule(ease, interval, repetitions, previousNext, now);

                srs.RecordAnswer(record, isCorrect: false, now);

                Assert.Equal(0, record.Repetitions);
                Assert.Equal(DomainRules.DefaultSrsIntervalDays, record.IntervalDays);
                Assert.True(record.IntervalDays < interval);
                Assert.True(record.NextReviewAt < previousNext);
                Assert.Equal(today.AddDays(record.IntervalDays), record.NextReviewAt);
                Assert.True(record.EaseFactor <= ease);
            },
            iter: 100,
            threads: 1);
    }
}
