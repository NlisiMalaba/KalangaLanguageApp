using Kalanga.Api.Authorization;
using Kalanga.Api.Contracts.Audio;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Domain;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kalanga.Api.Controllers;

[ApiController]
[Authorize]
[Route("audio")]
public sealed class AudioController : ControllerBase
{
    [HttpPost("upload")]
    [Authorize(Policy = AuthPolicies.ContributorOrAdmin)]
    [EnableRateLimiting(RateLimitPolicies.AudioUpload)]
    [RequestSizeLimit(DomainRules.MaxAudioFileSizeBytes + DomainRules.AudioUploadMultipartOverheadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = DomainRules.MaxAudioFileSizeBytes)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadAudioResult>> Upload(
        [FromForm] UploadAudioRequest request,
        [FromServices] UploadAudioPort uploadAudio,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.File is null || request.File.Length <= 0)
        {
            throw new InvalidAudioUploadException("Audio file is empty.");
        }

        await using var content = request.File.OpenReadStream();
        var result = await uploadAudio.ExecuteAsync(
            new UploadAudioCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                content,
                request.File.Length,
                request.DurationMs,
                request.PhraseId is { } phraseId && phraseId != Guid.Empty ? PhraseId.From(phraseId) : null,
                request.VariationId is { } variationId && variationId != Guid.Empty ? LanguageVariationId.From(variationId) : null,
                request.SpeakerGender,
                request.DialectLabel),
            cancellationToken);

        return Created($"/audio/{result.AudioRecordingId.Value}", result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetAudioResult>> Get(
        Guid id,
        [FromServices] GetAudioPort getAudio,
        CancellationToken cancellationToken)
    {
        var result = await getAudio.ExecuteAsync(
            new GetAudioCommand(User.GetLanguageId(), User.GetUserId(), AudioRecordingId.From(id)),
            cancellationToken);

        return Ok(result);
    }
}
