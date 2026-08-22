using CsCheck;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Services;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Tests.Domain;

public sealed class CorrectAnswerIncreasesSrsIntervalPropertyTests
{
    // Feature: kalanga-language-app, Property 15: Correct Answer Increases SRS Interval
    [Fact]
    public void Correct_answer_on_a_mature_card_increases_interval_and_delays_review()
    {
        var input =
            from easeCents in Gen.Int[130, 350]
            from interval in Gen.Int[1, 40]
            from repetitions in Gen.Int[2, 8]
            select (easeCents, interval, repetitions);

        var srs = new SpacedRepetitionService();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        Check.Sample(
            input,
            (int easeCents, int interval, int repetitions) =>
            {
                var ease = easeCents / 100m;
                var record = SpacedRepetitionRecord.Create(LanguageId.New(), UserId.New(), PhraseId.New(), now);
                record.Schedule(ease, interval, repetitions, today.AddDays(interval), now);

                srs.RecordAnswer(record, isCorrect: true, now);

                Assert.True(record.IntervalDays > interval);
                Assert.Equal(repetitions + 1, record.Repetitions);
                Assert.True(record.NextReviewAt > today);
                Assert.Equal(today.AddDays(record.IntervalDays), record.NextReviewAt);
                Assert.True(record.EaseFactor >= ease);
            },
            iter: 100,
            threads: 1);
    }
}
