using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class UpdateStreakUseCase(
    IUserRepository users,
    IGamificationRepository gamification,
    TimeProvider? time = null) : UpdateStreakPort
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public async Task<UpdateStreakResult> ExecuteAsync(
        UpdateStreakCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var now = _time.GetUtcNow();
        var existing = await gamification.FindByUserAsync(command.LanguageId, actor.Id, cancellationToken);
        var created = existing is null;
        var state = existing ?? LearnerGamification.Create(command.LanguageId, actor.Id, now);
        var streakBefore = state.CurrentStreak;

        Apply(state, command, now);
        state = await PersistAsync(command, state, created, now, cancellationToken);

        return new UpdateStreakResult(
            state.CurrentStreak,
            state.LongestStreak,
            state.LastActivityDate,
            Reset: !command.RecordActivity && streakBefore > 0 && state.CurrentStreak == 0);
    }

    private static void Apply(LearnerGamification state, UpdateStreakCommand command, DateTimeOffset now)
    {
        if (command.RecordActivity)
        {
            state.RecordActivity(command.AsOfDate, now);
            return;
        }

        state.ResetStreakIfMissed(command.AsOfDate, now);
    }

    private async Task<LearnerGamification> PersistAsync(
        UpdateStreakCommand command,
        LearnerGamification state,
        bool created,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!created)
        {
            await gamification.UpdateAsync(command.LanguageId, state, cancellationToken);
            return state;
        }

        if (await gamification.TryAddAsync(command.LanguageId, state, cancellationToken))
        {
            return state;
        }

        var winner = await gamification.FindByUserAsync(command.LanguageId, state.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Gamification row was not found after a unique conflict.");

        Apply(winner, command, now);
        await gamification.UpdateAsync(command.LanguageId, winner, cancellationToken);
        return winner;
    }
}
