using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class GetAudioUseCase(
    IUserRepository users,
    IAudioRecordingRepository recordings) : GetAudioPort
{
    public async Task<GetAudioResult> ExecuteAsync(
        GetAudioCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var recording = await recordings.FindByIdAsync(command.LanguageId, command.AudioRecordingId, cancellationToken)
            ?? throw new AudioRecordingNotFoundException(command.AudioRecordingId);

        if (!CanView(actor, recording))
        {
            throw new AudioRecordingNotFoundException(command.AudioRecordingId);
        }

        return new GetAudioResult(
            recording.Id,
            recording.LanguageId,
            recording.PhraseId,
            recording.VariationId,
            recording.CdnUrl,
            recording.FileFormat,
            recording.FileSizeBytes,
            recording.Status,
            recording.SpeakerGender,
            recording.DialectLabel,
            recording.DurationMs);
    }

    private static bool CanView(User actor, AudioRecording recording)
    {
        if (recording.IsPlayableByLearners)
        {
            return true;
        }

        return actor.Role switch
        {
            Role.Admin or Role.Reviewer => true,
            Role.Contributor => recording.ContributorId == actor.Id,
            _ => false,
        };
    }
}
