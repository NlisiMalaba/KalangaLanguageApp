using System.Buffers;
using Kalanga.Application.Audio;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = $"{nameof(Role.Contributor)},{nameof(Role.Admin)}")]
public sealed class UploadAudioUseCase(
    IUserRepository users,
    IPhraseRepository phrases,
    ILanguageVariationRepository variations,
    ILessonRepository lessons,
    IAudioRecordingRepository recordings,
    IAudioStorage storage) : UploadAudioPort
{
    public async Task<UploadAudioResult> ExecuteAsync(
        UploadAudioCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Content);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        LessonAuthoringGuard.EnsureCanAuthor(actor, "upload audio");

        if (command.DeclaredContentLength is { } declared
            && (declared <= 0 || declared > DomainRules.MaxAudioFileSizeBytes))
        {
            throw new InvalidAudioUploadException(
                $"Audio must be between 1 and {DomainRules.MaxAudioFileSizeBytes} bytes.");
        }

        var phraseId = await ResolvePhraseIdAsync(command, cancellationToken);
        var phrase = await phrases.FindByIdAsync(command.LanguageId, phraseId, cancellationToken)
            ?? throw new PhraseNotFoundException(phraseId);

        var lesson = await lessons.FindByIdAsync(command.LanguageId, phrase.LessonId, cancellationToken)
            ?? throw new LessonNotFoundException(phrase.LessonId);

        LessonAuthoringGuard.EnsureCanMutate(actor, lesson, "upload audio");

        await using var payload = await CopyCappedAsync(command.Content, cancellationToken);
        var format = DetectFormat(payload);
        var recordingId = AudioRecordingId.New();
        var stored = false;

        try
        {
            var cdnUrl = await storage.PutAsync(
                command.LanguageId,
                phraseId,
                recordingId,
                format,
                payload,
                (int)payload.Length,
                cancellationToken);
            stored = true;

            var recording = AudioRecording.Create(
                command.LanguageId,
                actor.Id,
                cdnUrl,
                format,
                (int)payload.Length,
                command.DurationMs,
                DateTimeOffset.UtcNow,
                phraseId,
                command.VariationId,
                command.SpeakerGender,
                command.DialectLabel,
                recordingId);

            await recordings.AddAsync(command.LanguageId, recording, cancellationToken);

            return new UploadAudioResult(
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
        catch
        {
            if (stored)
            {
                await storage.TryDeleteAsync(
                    command.LanguageId,
                    phraseId,
                    recordingId,
                    format,
                    cancellationToken);
            }

            throw;
        }
    }

    private async Task<PhraseId> ResolvePhraseIdAsync(UploadAudioCommand command, CancellationToken cancellationToken)
    {
        if (command.VariationId is { } variationId)
        {
            var variation = await variations.FindByIdAsync(command.LanguageId, variationId, cancellationToken)
                ?? throw new VariationNotFoundException(variationId);

            if (command.PhraseId is { } expectedPhrase && expectedPhrase != variation.PhraseId)
            {
                throw new InvalidAudioUploadException("The variation does not belong to the given phrase.");
            }

            return variation.PhraseId;
        }

        return command.PhraseId ?? throw new InvalidAudioUploadException(
            "Audio must be associated with a phrase or a language variation.");
    }

    private static AudioFileFormat DetectFormat(MemoryStream payload)
    {
        payload.Position = 0;
        Span<byte> header = stackalloc byte[16];
        var read = payload.Read(header);
        payload.Position = 0;
        return AudioFormatSniffer.Detect(header[..read]);
    }

    private static async Task<MemoryStream> CopyCappedAsync(Stream source, CancellationToken cancellationToken)
    {
        var payload = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(80 * 1024);
        try
        {
            var total = 0;
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                total += read;
                if (total > DomainRules.MaxAudioFileSizeBytes)
                {
                    throw new InvalidAudioUploadException(
                        $"Audio must be between 1 and {DomainRules.MaxAudioFileSizeBytes} bytes.");
                }

                await payload.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            if (total == 0)
            {
                throw new InvalidAudioUploadException("Audio file is empty.");
            }

            payload.Position = 0;
            return payload;
        }
        catch
        {
            await payload.DisposeAsync();
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
