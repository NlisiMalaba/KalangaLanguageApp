using Kalanga.Application.Dtos;
using Kalanga.Domain.Entities;

namespace Kalanga.Application.Sync;

internal static class SyncChangeMapper
{
    public static SyncChangesDto ToDto(
        IReadOnlyList<LearnerProgress> progress,
        IReadOnlyList<SpacedRepetitionRecord> spacedRepetition,
        LearnerGamification? gamification)
    {
        return new SyncChangesDto(
            progress.Select(static item => new SyncProgressItemDto(
                item.LessonId,
                item.CompletedAt,
                item.Score,
                item.XpAwarded,
                item.UpdatedAt)).ToList(),
            spacedRepetition.Select(static item => new SyncSrsItemDto(
                item.PhraseId,
                item.VariationId,
                item.EaseFactor,
                item.IntervalDays,
                item.Repetitions,
                item.NextReviewAt,
                item.LastReviewedAt,
                item.UpdatedAt)).ToList(),
            gamification is null
                ? null
                : new SyncGamificationDto(
                    gamification.TotalXp,
                    gamification.CurrentStreak,
                    gamification.LongestStreak,
                    gamification.LastActivityDate,
                    gamification.ProgressLevel,
                    XpDelta: 0,
                    gamification.UpdatedAt));
    }
}
