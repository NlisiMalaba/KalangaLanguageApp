using Kalanga.Api.Authorization;
using Kalanga.Api.Contracts.Sync;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Domain;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[Authorize]
[Route("sync")]
public sealed class SyncController : ControllerBase
{
    [HttpPost("push")]
    [RequestSizeLimit(DomainRules.MaxSyncPayloadBytes)]
    public async Task<ActionResult<SyncProgressResult>> Push(
        [FromBody] SyncPushRequest request,
        [FromServices] SyncProgressPort syncProgress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await syncProgress.ExecuteAsync(
            new SyncProgressCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                request.ClientOperationId,
                (request.Progress ?? []).Select(static item => new SyncProgressItemDto(
                    LessonId.From(item.LessonId),
                    item.CompletedAt,
                    item.Score,
                    item.XpAwarded,
                    item.UpdatedAt)).ToList(),
                (request.SpacedRepetition ?? []).Select(static item => new SyncSrsItemDto(
                    PhraseId.From(item.PhraseId),
                    item.VariationId is { } variationId && variationId != Guid.Empty
                        ? LanguageVariationId.From(variationId)
                        : null,
                    item.EaseFactor,
                    item.IntervalDays,
                    item.Repetitions,
                    item.NextReviewAt,
                    item.LastReviewedAt,
                    item.UpdatedAt)).ToList(),
                request.Gamification is null
                    ? null
                    : new SyncGamificationDto(
                        request.Gamification.TotalXp,
                        request.Gamification.CurrentStreak,
                        request.Gamification.LongestStreak,
                        request.Gamification.LastActivityDate,
                        Level.Beginner,
                        request.Gamification.XpDelta,
                        request.Gamification.UpdatedAt)),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("pull")]
    public async Task<ActionResult<PullSyncResult>> Pull(
        [FromServices] PullSyncPort pullSync,
        [FromQuery] long since = 0,
        CancellationToken cancellationToken = default)
    {
        var result = await pullSync.ExecuteAsync(
            new PullSyncCommand(User.GetLanguageId(), User.GetUserId(), since),
            cancellationToken);

        return Ok(result);
    }
}
