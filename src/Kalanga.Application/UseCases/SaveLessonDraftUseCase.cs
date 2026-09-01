using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = $"{nameof(Role.Contributor)},{nameof(Role.Admin)}")]
public sealed class SaveLessonDraftUseCase(
    IUserRepository users,
    ILessonRepository lessons,
    IPhraseRepository phrases,
    ILanguageVariationRepository variations,
    IExerciseRepository exercises,
    IUnitOfWork unitOfWork) : SaveLessonDraftPort
{
    public async Task<SaveLessonDraftResult> ExecuteAsync(
        SaveLessonDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await unitOfWork.ExecuteInTransactionAsync(
            ct => SaveAsync(command, ct),
            cancellationToken);
    }

    private async Task<SaveLessonDraftResult> SaveAsync(
        SaveLessonDraftCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        var lesson = await lessons.FindByIdAsync(command.LanguageId, command.LessonId, cancellationToken)
            ?? throw new LessonNotFoundException(command.LessonId);

        LessonAuthoringGuard.EnsureCanMutate(actor, lesson, "save a draft");

        var now = DateTimeOffset.UtcNow;
        lesson.UpdateDraft(
            command.Title,
            command.Level,
            command.Category,
            command.IsScenario,
            command.ScenarioContext,
            command.XpReward,
            now);

        await lessons.UpdateAsync(command.LanguageId, lesson, cancellationToken);

        if (command.Phrases is not null)
        {
            await UpsertPhrasesAsync(command, now, cancellationToken);
        }

        if (command.Exercises is not null)
        {
            await UpsertExercisesAsync(command, now, cancellationToken);
        }

        return new SaveLessonDraftResult(
            lesson.Id,
            lesson.LanguageId,
            lesson.Title,
            lesson.Level,
            lesson.Category,
            lesson.IsScenario,
            lesson.ScenarioContext,
            lesson.Status,
            lesson.ContributorId,
            lesson.XpReward);
    }

    private async Task UpsertPhrasesAsync(
        SaveLessonDraftCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var item in command.Phrases ?? [])
        {
            Phrase phrase;
            if (item.PhraseId is { } phraseGuid && phraseGuid != Guid.Empty)
            {
                var phraseId = PhraseId.From(phraseGuid);
                var existing = await phrases.FindByIdAsync(command.LanguageId, phraseId, cancellationToken)
                    ?? throw new PhraseNotFoundException(phraseId);
                if (existing.LessonId != command.LessonId)
                {
                    throw new LessonAccessDeniedException("save a draft phrase");
                }

                existing.Update(item.KalangaText, item.EnglishTranslation, item.SortOrder);
                await phrases.UpdateAsync(command.LanguageId, existing, cancellationToken);
                phrase = existing;
            }
            else
            {
                phrase = Phrase.Create(
                    command.LanguageId,
                    command.LessonId,
                    item.KalangaText,
                    item.EnglishTranslation,
                    item.SortOrder,
                    now);
                await phrases.AddAsync(command.LanguageId, phrase, cancellationToken);
            }

            foreach (var variationItem in item.Variations ?? [])
            {
                if (variationItem.VariationId is { } variationGuid && variationGuid != Guid.Empty)
                {
                    var variationId = LanguageVariationId.From(variationGuid);
                    var existingVariation = await variations.FindByIdAsync(
                        command.LanguageId,
                        variationId,
                        cancellationToken)
                        ?? throw new VariationNotFoundException(variationId);
                    if (existingVariation.PhraseId != phrase.Id)
                    {
                        throw new LessonAccessDeniedException("save a draft variation");
                    }

                    existingVariation.Update(variationItem.KalangaText, variationItem.RegisterLabel);
                    await variations.UpdateAsync(command.LanguageId, existingVariation, cancellationToken);
                    continue;
                }

                var created = LanguageVariation.Create(
                    command.LanguageId,
                    phrase.Id,
                    variationItem.KalangaText,
                    variationItem.RegisterLabel,
                    now);
                await variations.AddAsync(command.LanguageId, created, cancellationToken);
            }
        }
    }

    private async Task UpsertExercisesAsync(
        SaveLessonDraftCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var item in command.Exercises ?? [])
        {
            if (item.ExerciseId is { } exerciseGuid && exerciseGuid != Guid.Empty)
            {
                var exerciseId = ExerciseId.From(exerciseGuid);
                var existing = await exercises.FindByIdAsync(command.LanguageId, exerciseId, cancellationToken)
                    ?? throw new ExerciseNotFoundException(exerciseId);
                if (existing.LessonId != command.LessonId)
                {
                    throw new LessonAccessDeniedException("save a draft exercise");
                }

                existing.Update(item.ExerciseType, item.PromptData, item.CorrectAnswer, item.SortOrder);
                await exercises.UpdateAsync(command.LanguageId, existing, cancellationToken);
                continue;
            }

            var created = Exercise.Create(
                command.LanguageId,
                command.LessonId,
                item.ExerciseType,
                item.PromptData,
                item.CorrectAnswer,
                item.SortOrder,
                now);
            await exercises.AddAsync(command.LanguageId, created, cancellationToken);
        }
    }
}
