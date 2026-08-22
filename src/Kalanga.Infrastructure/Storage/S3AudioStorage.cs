using Amazon.S3;
using Amazon.S3.Model;
using Kalanga.Application.Audio;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Kalanga.Infrastructure.Storage;

internal sealed class S3AudioStorage(IAmazonS3 s3, IOptions<AudioStorageOptions> options) : IAudioStorage
{
    public async Task<string> PutAsync(
        LanguageId languageId,
        PhraseId phraseId,
        AudioRecordingId recordingId,
        AudioFileFormat format,
        Stream content,
        int contentLength,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Bucket) || string.IsNullOrWhiteSpace(settings.CdnBaseUrl))
        {
            throw new AudioStorageUnavailableException("Audio storage is not configured.");
        }

        var key = AudioObjectKeys.For(languageId, phraseId, recordingId, format);
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        try
        {
            await s3.PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = settings.Bucket,
                    Key = key,
                    InputStream = content,
                    Headers = { ContentLength = contentLength },
                    ContentType = AudioObjectKeys.ContentType(format),
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                    AutoCloseStream = false,
                },
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new AudioStorageUnavailableException("Failed to store audio object.");
        }

        return $"{settings.CdnBaseUrl.TrimEnd('/')}/{key}";
    }

    public async Task TryDeleteAsync(
        LanguageId languageId,
        PhraseId phraseId,
        AudioRecordingId recordingId,
        AudioFileFormat format,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Bucket))
        {
            return;
        }

        try
        {
            await s3.DeleteObjectAsync(
                settings.Bucket,
                AudioObjectKeys.For(languageId, phraseId, recordingId, format),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Best-effort cleanup after a failed persist. Do not fail the original error.
        }
    }
}
