using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = $"{nameof(Role.Contributor)},{nameof(Role.Admin)}")]
public sealed class GetLessonDraftUseCase(
    IUserRepository users,
    ILessonRepository lessons,
    IPhraseRepository phrases,
    ILanguageVariationRepository variations,
    IAudioRecordingRepository audio,
    IExerciseRepository exercises) : GetLessonDraftPort
{
    public async Task<GetLessonDraftResult> ExecuteAsync(
        GetLessonDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        var lesson = await lessons.FindByIdAsync(command.LanguageId, command.LessonId, cancellationToken)
            ?? throw new LessonNotFoundException(command.LessonId);

        LessonAuthoringGuard.EnsureCanMutate(actor, lesson, "view a draft");

        var detail = await LessonDetailComposer.ComposeAsync(
            command.LanguageId,
            lesson,
            phrases,
            variations,
            audio,
            exercises,
            cancellationToken,
            includeUnapprovedAudio: true);

        return new GetLessonDraftResult(detail, lesson.Status);
    }
}
