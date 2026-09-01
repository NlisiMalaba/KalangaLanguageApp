using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class GetLessonUseCase(
    ILessonRepository lessons,
    IPhraseRepository phrases,
    ILanguageVariationRepository variations,
    IAudioRecordingRepository audio,
    IExerciseRepository exercises) : GetLessonPort
{
    public async Task<GetLessonResult> ExecuteAsync(
        GetLessonCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var lesson = await lessons.FindByIdAsync(command.LanguageId, command.LessonId, cancellationToken);
        if (lesson is null || !lesson.IsVisibleInCatalog)
        {
            throw new LessonNotFoundException(command.LessonId);
        }

        var detail = await LessonDetailComposer.ComposeAsync(
            command.LanguageId,
            lesson,
            phrases,
            variations,
            audio,
            exercises,
            cancellationToken);

        return new GetLessonResult(detail);
    }
}
