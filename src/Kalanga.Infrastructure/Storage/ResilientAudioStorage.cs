using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Kalanga.Infrastructure.Storage;

internal sealed class ResilientAudioStorage : IAudioStorage
{
    private readonly IAudioStorage _inner;
    private readonly SemaphoreSlim _bulkhead;
    private readonly TimeSpan _timeout;
    private readonly ConsecutiveFailureCircuit _circuit = new();

    public ResilientAudioStorage(S3AudioStorage inner, IOptions<AudioStorageOptions> options)
    {
        _inner = inner;
        var settings = options.Value;
        _timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));
        _bulkhead = new SemaphoreSlim(Math.Max(1, settings.MaxConcurrentUploads));
    }

    public Task<string> PutAsync(
        LanguageId languageId,
        PhraseId phraseId,
        AudioRecordingId recordingId,
        AudioFileFormat format,
        Stream content,
        int contentLength,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            ct => _inner.PutAsync(languageId, phraseId, recordingId, format, content, contentLength, ct),
            cancellationToken);

    public async Task TryDeleteAsync(
        LanguageId languageId,
        PhraseId phraseId,
        AudioRecordingId recordingId,
        AudioFileFormat format,
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            async ct =>
            {
                await _inner.TryDeleteAsync(languageId, phraseId, recordingId, format, ct);
                return true;
            },
            cancellationToken);

    private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        if (_circuit.IsOpen)
        {
            throw new AudioStorageUnavailableException("Audio storage is temporarily unavailable.");
        }

        if (!await _bulkhead.WaitAsync(_timeout, cancellationToken))
        {
            throw new AudioStorageUnavailableException("Audio storage is busy. Retry later.");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_timeout);
            var result = await operation(timeout.Token);
            _circuit.OnSuccess();
            return result;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _circuit.OnFailure();
            if (exception is AudioStorageUnavailableException)
            {
                throw;
            }

            throw new AudioStorageUnavailableException("Audio storage request failed.");
        }
        finally
        {
            _bulkhead.Release();
        }
    }
}

internal sealed class ConsecutiveFailureCircuit
{
    private const int Threshold = 5;
    private static readonly TimeSpan OpenDuration = TimeSpan.FromSeconds(30);

    private int _consecutiveFailures;
    private long _openUntilTicks;

    public bool IsOpen => DateTimeOffset.UtcNow.Ticks < Interlocked.Read(ref _openUntilTicks);

    public void OnSuccess() => Interlocked.Exchange(ref _consecutiveFailures, 0);

    public void OnFailure()
    {
        if (Interlocked.Increment(ref _consecutiveFailures) >= Threshold)
        {
            Interlocked.Exchange(ref _openUntilTicks, DateTimeOffset.UtcNow.Add(OpenDuration).Ticks);
        }
    }
}
