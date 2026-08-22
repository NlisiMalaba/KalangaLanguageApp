using Kalanga.Domain.Entities;

namespace Kalanga.Domain.Services;

public sealed class SpacedRepetitionService
{
    public const int CorrectQuality = 5;

    public const int IncorrectQuality = 0;

    public void RecordAnswer(SpacedRepetitionRecord record, bool isCorrect, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(record);

        var quality = isCorrect ? CorrectQuality : IncorrectQuality;
        var ease = AdjustEase(record.EaseFactor, quality);
        var interval = record.IntervalDays;
        var repetitions = record.Repetitions;

        if (quality < DomainRules.SrsPassingQuality)
        {
            repetitions = 0;
            interval = DomainRules.DefaultSrsIntervalDays;
        }
        else
        {
            interval = repetitions switch
            {
                0 => DomainRules.DefaultSrsIntervalDays,
                1 => DomainRules.SrsSecondIntervalDays,
                _ => NextInterval(interval, ease),
            };
            repetitions++;
        }

        var today = DateOnly.FromDateTime(utcNow.UtcDateTime);
        record.Schedule(ease, interval, repetitions, today.AddDays(interval), utcNow);
    }

    private static int NextInterval(int intervalDays, decimal easeFactor)
    {
        var next = (int)decimal.Round(intervalDays * easeFactor, MidpointRounding.AwayFromZero);
        return Math.Max(intervalDays + 1, next);
    }

    private static decimal AdjustEase(decimal easeFactor, int quality)
    {
        var delta = 5 - quality;
        var adjusted = easeFactor + (0.1m - delta * (0.08m + delta * 0.02m));
        adjusted = decimal.Round(adjusted, 2, MidpointRounding.AwayFromZero);
        if (adjusted < DomainRules.MinEaseFactor)
        {
            return DomainRules.MinEaseFactor;
        }

        return adjusted > DomainRules.MaxEaseFactor ? DomainRules.MaxEaseFactor : adjusted;
    }
}
