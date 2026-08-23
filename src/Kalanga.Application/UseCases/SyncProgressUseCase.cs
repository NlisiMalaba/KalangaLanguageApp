using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Application.Sync;
using Kalanga.Domain;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class SyncProgressUseCase(
    IUserRepository users,
    ILessonRepository lessons,
    IPhraseRepository phrases,
    ILanguageVariationRepository variations,
    ILearnerProgressRepository progress,
    ISpacedRepetitionRepository spacedRepetition,
    IGamificationRepository gamification,
    ISyncCheckpointRepository checkpoints,
    ISyncPushReceiptRepository receipts,
    IUnitOfWork unitOfWork,
    TimeProvider? time = null) : SyncProgressPort
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public async Task<SyncProgressResult> ExecuteAsync(
        SyncProgressCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        command = command with { ClientOperationId = command.ClientOperationId?.Trim() ?? string.Empty };
        ValidatePayload(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        await EnsureReferencedContentExistsAsync(command, cancellationToken);

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(
                ct => ApplyAsync(command, actor.Id, ct),
                cancellationToken);
        }
        catch (DuplicateSyncPushException)
        {
            return await LoadResultAsync(command.LanguageId, actor.Id, idempotentReplay: true, cancellationToken);
        }
    }

    private async Task<SyncProgressResult> ApplyAsync(
        SyncProgressCommand command,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (await receipts.ExistsAsync(command.LanguageId, userId, command.ClientOperationId, cancellationToken))
        {
            return await LoadResultAsync(command.LanguageId, userId, idempotentReplay: true, cancellationToken);
        }

        var now = _time.GetUtcNow();
        var xpFromLessons = await ApplyProgressAsync(command, userId, cancellationToken);
        await ApplySrsAsync(command, userId, cancellationToken);
        await ApplyGamificationAsync(command, userId, xpFromLessons, now, cancellationToken);

        await receipts.AddAsync(
            command.LanguageId,
            SyncPushReceipt.Create(command.LanguageId, userId, command.ClientOperationId, now),
            cancellationToken);

        var checkpoint = await checkpoints.FindByUserAsync(command.LanguageId, userId, cancellationToken);
        if (checkpoint is null)
        {
            checkpoint = SyncCheckpoint.Create(command.LanguageId, userId, now);
            checkpoint.Advance(1, now);
            await checkpoints.AddAsync(command.LanguageId, checkpoint, cancellationToken);
        }
        else
        {
            checkpoint.Advance(checkpoint.SyncVersion + 1, now);
            await checkpoints.UpdateAsync(command.LanguageId, checkpoint, cancellationToken);
        }

        return await LoadResultAsync(command.LanguageId, userId, idempotentReplay: false, cancellationToken);
    }

    private async Task<int> ApplyProgressAsync(
        SyncProgressCommand command,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var existing = (await progress.FindByUserAsync(command.LanguageId, userId, cancellationToken))
            .ToDictionary(static item => item.LessonId);
        var xpGranted = 0;

        foreach (var item in command.Progress)
        {
            if (existing.TryGetValue(item.LessonId, out var recorded))
            {
                var wasCompleted = recorded.IsCompleted;
                if (!recorded.ApplyLastWriteWins(item.CompletedAt, item.Score, item.XpAwarded, item.UpdatedAt))
                {
                    continue;
                }

                await progress.UpdateAsync(command.LanguageId, recorded, cancellationToken);
                if (!wasCompleted && recorded.IsCompleted)
                {
                    xpGranted += recorded.XpAwarded;
                }

                continue;
            }

            var created = LearnerProgress.FromClient(
                command.LanguageId,
                userId,
                item.LessonId,
                item.CompletedAt,
                item.Score,
                item.XpAwarded,
                item.UpdatedAt);
            await progress.AddAsync(command.LanguageId, created, cancellationToken);
            existing[item.LessonId] = created;
            if (created.IsCompleted)
            {
                xpGranted += created.XpAwarded;
            }
        }

        return xpGranted;
    }

    private async Task ApplySrsAsync(
        SyncProgressCommand command,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var existing = (await spacedRepetition.FindByUserAsync(command.LanguageId, userId, cancellationToken))
            .ToDictionary(static item => (item.PhraseId, item.VariationId));

        foreach (var item in command.SpacedRepetition)
        {
            var key = (item.PhraseId, item.VariationId);
            if (existing.TryGetValue(key, out var recorded))
            {
                if (!recorded.ApplyLastWriteWins(
                        item.EaseFactor,
                        item.IntervalDays,
                        item.Repetitions,
                        item.NextReviewAt,
                        item.LastReviewedAt,
                        item.UpdatedAt))
                {
                    continue;
                }

                await spacedRepetition.UpdateAsync(command.LanguageId, recorded, cancellationToken);
                continue;
            }

            var created = SpacedRepetitionRecord.FromClient(
                command.LanguageId,
                userId,
                item.PhraseId,
                item.VariationId,
                item.EaseFactor,
                item.IntervalDays,
                item.Repetitions,
                item.NextReviewAt,
                item.LastReviewedAt,
                item.UpdatedAt);
            await spacedRepetition.AddAsync(command.LanguageId, created, cancellationToken);
            existing[key] = created;
        }
    }

    private async Task ApplyGamificationAsync(
        SyncProgressCommand command,
        UserId userId,
        int xpFromLessons,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var additionalXp = xpFromLessons + Math.Max(0, command.Gamification?.XpDelta ?? 0);
        var incoming = command.Gamification;
        if (additionalXp == 0 && incoming is null)
        {
            return;
        }

        var state = await gamification.FindByUserAsync(command.LanguageId, userId, cancellationToken);
        var created = state is null;
        state ??= LearnerGamification.Create(command.LanguageId, userId, now);
        state.MergeAdditive(
            additionalXp,
            incoming?.CurrentStreak ?? 0,
            incoming?.LongestStreak ?? 0,
            incoming?.LastActivityDate,
            now);

        if (created)
        {
            await gamification.AddAsync(command.LanguageId, state, cancellationToken);
            return;
        }

        await gamification.UpdateAsync(command.LanguageId, state, cancellationToken);
    }

    private async Task EnsureReferencedContentExistsAsync(
        SyncProgressCommand command,
        CancellationToken cancellationToken)
    {
        var lessonIds = command.Progress.Select(static item => item.LessonId).Distinct().ToArray();
        if (lessonIds.Length > 0)
        {
            var found = await lessons.FindByIdsAsync(command.LanguageId, lessonIds, cancellationToken);
            if (found.Count != lessonIds.Length)
            {
                throw new InvalidSyncPayloadException("One or more lessons in the sync payload were not found.");
            }
        }

        var phraseIds = command.SpacedRepetition.Select(static item => item.PhraseId).Distinct().ToArray();
        if (phraseIds.Length == 0)
        {
            return;
        }

        var phrasesFound = await phrases.FindByIdsAsync(command.LanguageId, phraseIds, cancellationToken);
        if (phrasesFound.Count != phraseIds.Length)
        {
            throw new InvalidSyncPayloadException("One or more phrases in the sync payload were not found.");
        }

        var variationIds = command.SpacedRepetition
            .Where(static item => item.VariationId is not null)
            .Select(static item => item.VariationId!.Value)
            .Distinct()
            .ToArray();
        if (variationIds.Length == 0)
        {
            return;
        }

        var knownVariations = (await variations.FindByPhraseIdsAsync(command.LanguageId, phraseIds, cancellationToken))
            .Select(static item => item.Id)
            .ToHashSet();
        if (variationIds.Any(id => !knownVariations.Contains(id)))
        {
            throw new InvalidSyncPayloadException("One or more language variations in the sync payload were not found.");
        }
    }

    private async Task<SyncProgressResult> LoadResultAsync(
        LanguageId languageId,
        UserId userId,
        bool idempotentReplay,
        CancellationToken cancellationToken)
    {
        var checkpoint = await checkpoints.FindByUserAsync(languageId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Sync checkpoint was missing after a successful push.");
        var changes = SyncChangeMapper.ToDto(
            await progress.FindByUserAsync(languageId, userId, cancellationToken),
            await spacedRepetition.FindByUserAsync(languageId, userId, cancellationToken),
            await gamification.FindByUserAsync(languageId, userId, cancellationToken));
        return new SyncProgressResult(checkpoint.SyncVersion, checkpoint.LastSyncedAt, idempotentReplay, changes);
    }

    private static void ValidatePayload(SyncProgressCommand command)
    {
        var clientOperationId = command.ClientOperationId?.Trim();
        if (string.IsNullOrWhiteSpace(clientOperationId)
            || clientOperationId.Length > DomainRules.MaxSyncClientOperationIdLength)
        {
            throw new InvalidSyncPayloadException("A client operation id is required for sync push.");
        }

        if (command.Progress.Count > DomainRules.MaxSyncProgressItems)
        {
            throw new InvalidSyncPayloadException(
                $"Sync push cannot include more than {DomainRules.MaxSyncProgressItems} progress items.");
        }

        if (command.SpacedRepetition.Count > DomainRules.MaxSyncSrsItems)
        {
            throw new InvalidSyncPayloadException(
                $"Sync push cannot include more than {DomainRules.MaxSyncSrsItems} SRS items.");
        }

        if (command.Progress.Select(static item => item.LessonId).Distinct().Count() != command.Progress.Count)
        {
            throw new InvalidSyncPayloadException("Sync push contains duplicate lesson progress items.");
        }

        if (command.SpacedRepetition
                .Select(static item => (item.PhraseId, item.VariationId))
                .Distinct()
                .Count()
            != command.SpacedRepetition.Count)
        {
            throw new InvalidSyncPayloadException("Sync push contains duplicate SRS items.");
        }

        foreach (var item in command.Progress)
        {
            if (item.Score is < 0 or > DomainRules.MaxLessonScore)
            {
                throw new InvalidSyncPayloadException(
                    $"Score must be between 0 and {DomainRules.MaxLessonScore}.");
            }

            if (item.XpAwarded < 0)
            {
                throw new InvalidSyncPayloadException("XP awarded cannot be negative.");
            }
        }

        if (command.Gamification is { XpDelta: < 0 })
        {
            throw new InvalidSyncPayloadException("Gamification XP delta cannot be negative.");
        }
    }
}
