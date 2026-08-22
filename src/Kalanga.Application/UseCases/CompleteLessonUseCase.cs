using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class CompleteLessonUseCase(
    IUserRepository users,
    ILessonRepository lessons,
    ILearnerProgressRepository progress,
    IGamificationRepository gamification,
    TimeProvider? time = null) : CompleteLessonPort
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public async Task<CompleteLessonResult> ExecuteAsync(
        CompleteLessonCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Score is < 0 or > DomainRules.MaxLessonScore)
        {
            throw new InvalidLessonCompletionException(
                $"Score must be between 0 and {DomainRules.MaxLessonScore}.");
        }

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var lesson = await lessons.FindByIdAsync(command.LanguageId, command.LessonId, cancellationToken);
        if (lesson is null || !lesson.IsVisibleInCatalog)
        {
            throw new LessonNotFoundException(command.LessonId);
        }

        var now = _time.GetUtcNow();
        var existing = await progress.FindByUserAndLessonAsync(
            command.LanguageId,
            actor.Id,
            lesson.Id,
            cancellationToken);

        var xpGranted = false;
        LearnerProgress recorded;

        if (existing is null)
        {
            recorded = LearnerProgress.Start(command.LanguageId, actor.Id, lesson.Id, now);
            recorded.Complete(command.Score, lesson.XpReward, now);
            try
            {
                await progress.AddAsync(command.LanguageId, recorded, cancellationToken);
                xpGranted = lesson.XpReward > 0;
            }
            catch (DuplicateLearnerProgressException)
            {
                recorded = await progress.FindByUserAndLessonAsync(
                    command.LanguageId,
                    actor.Id,
                    lesson.Id,
                    cancellationToken)
                    ?? throw new DuplicateLearnerProgressException();

                xpGranted = ApplyRepeatOrFinish(recorded, command.Score, lesson.XpReward, now);
                await progress.UpdateAsync(command.LanguageId, recorded, cancellationToken);
            }
        }
        else
        {
            recorded = existing;
            xpGranted = ApplyRepeatOrFinish(recorded, command.Score, lesson.XpReward, now);
            await progress.UpdateAsync(command.LanguageId, recorded, cancellationToken);
        }

        var totals = await ApplyXpAsync(command.LanguageId, actor.Id, lesson.XpReward, xpGranted, now, cancellationToken);

        return new CompleteLessonResult(
            lesson.Id,
            recorded.CompletedAt ?? now,
            recorded.Score ?? command.Score,
            recorded.XpAwarded,
            totals,
            xpGranted);
    }

    private static bool ApplyRepeatOrFinish(LearnerProgress recorded, int score, int lessonXp, DateTimeOffset now)
    {
        if (recorded.IsCompleted)
        {
            recorded.Complete(score, recorded.XpAwarded, now);
            return false;
        }

        recorded.Complete(score, lessonXp, now);
        return lessonXp > 0;
    }

    private async Task<int> ApplyXpAsync(
        LanguageId languageId,
        UserId userId,
        int lessonXp,
        bool xpGranted,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var state = await gamification.FindByUserAsync(languageId, userId, cancellationToken);
        var created = state is null;
        state ??= LearnerGamification.Create(languageId, userId, now);

        if (xpGranted)
        {
            state.AwardXp(lessonXp, now);
        }

        state.RecordActivity(DateOnly.FromDateTime(now.UtcDateTime), now);
        state.AdvanceLevelIfThresholdCrossed(now);

        if (created && await gamification.TryAddAsync(languageId, state, cancellationToken))
        {
            return state.TotalXp;
        }

        if (created)
        {
            state = await gamification.FindByUserAsync(languageId, userId, cancellationToken)
                ?? throw new InvalidOperationException("Gamification row was not found after a unique conflict.");

            if (xpGranted)
            {
                state.AwardXp(lessonXp, now);
            }

            state.RecordActivity(DateOnly.FromDateTime(now.UtcDateTime), now);
            state.AdvanceLevelIfThresholdCrossed(now);
        }

        await gamification.UpdateAsync(languageId, state, cancellationToken);
        return state.TotalXp;
    }
}
