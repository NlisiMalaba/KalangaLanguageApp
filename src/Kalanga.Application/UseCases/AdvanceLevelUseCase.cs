using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class AdvanceLevelUseCase(
    IUserRepository users,
    IGamificationRepository gamification,
    TimeProvider? time = null) : AdvanceLevelPort
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public async Task<AdvanceLevelResult> ExecuteAsync(
        AdvanceLevelCommand command,
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
        var advanced = state.AdvanceLevelIfThresholdCrossed(now);

        if (created)
        {
            if (!await gamification.TryAddAsync(command.LanguageId, state, cancellationToken))
            {
                state = await gamification.FindByUserAsync(command.LanguageId, actor.Id, cancellationToken)
                    ?? throw new InvalidOperationException("Gamification row was not found after a unique conflict.");
                advanced = state.AdvanceLevelIfThresholdCrossed(now);
                await gamification.UpdateAsync(command.LanguageId, state, cancellationToken);
            }
        }
        else if (advanced)
        {
            await gamification.UpdateAsync(command.LanguageId, state, cancellationToken);
        }

        return new AdvanceLevelResult(state.ProgressLevel, state.TotalXp, advanced);
    }
}
