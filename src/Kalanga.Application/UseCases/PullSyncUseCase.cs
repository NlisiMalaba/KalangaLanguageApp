using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Application.Sync;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class PullSyncUseCase(
    IUserRepository users,
    ILearnerProgressRepository progress,
    ISpacedRepetitionRepository spacedRepetition,
    IGamificationRepository gamification,
    ISyncCheckpointRepository checkpoints,
    TimeProvider? time = null) : PullSyncPort
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public async Task<PullSyncResult> ExecuteAsync(
        PullSyncCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.SinceVersion < 0)
        {
            throw new InvalidSyncPayloadException("since must be a non-negative sync version.");
        }

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var checkpoint = await checkpoints.FindByUserAsync(command.LanguageId, actor.Id, cancellationToken);
        var now = _time.GetUtcNow();
        DateTimeOffset? updatedAfter = null;
        if (checkpoint is not null && command.SinceVersion >= checkpoint.SyncVersion)
        {
            updatedAfter = checkpoint.LastSyncedAt;
        }

        var progressItems = updatedAfter is { } watermark
            ? await progress.FindUpdatedSinceAsync(command.LanguageId, actor.Id, watermark, cancellationToken)
            : await progress.FindByUserAsync(command.LanguageId, actor.Id, cancellationToken);
        var srsItems = updatedAfter is { } srsWatermark
            ? await spacedRepetition.FindUpdatedSinceAsync(command.LanguageId, actor.Id, srsWatermark, cancellationToken)
            : await spacedRepetition.FindByUserAsync(command.LanguageId, actor.Id, cancellationToken);
        var totals = await gamification.FindByUserAsync(command.LanguageId, actor.Id, cancellationToken);
        if (updatedAfter is { } gamificationWatermark
            && totals is not null
            && totals.UpdatedAt <= gamificationWatermark)
        {
            totals = null;
        }

        if (checkpoint is null)
        {
            checkpoint = SyncCheckpoint.Create(command.LanguageId, actor.Id, now);
            await checkpoints.AddAsync(command.LanguageId, checkpoint, cancellationToken);
        }
        else
        {
            checkpoint.Advance(checkpoint.SyncVersion, now);
            await checkpoints.UpdateAsync(command.LanguageId, checkpoint, cancellationToken);
        }

        return new PullSyncResult(
            checkpoint.SyncVersion,
            checkpoint.LastSyncedAt,
            SyncChangeMapper.ToDto(progressItems, srsItems, totals));
    }
}
