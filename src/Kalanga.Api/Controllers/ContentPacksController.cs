using Kalanga.Api.Authorization;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[Authorize]
[Route("content-packs")]
public sealed class ContentPacksController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ListContentPacksResult>> List(
        [FromServices] ListContentPacksPort listPacks,
        [FromQuery(Name = "language_id")] Guid? languageId,
        [FromQuery] Level? level,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var tenant = User.GetLanguageId();
        var result = await listPacks.ExecuteAsync(
            new ListContentPacksCommand(
                tenant,
                level,
                category,
                languageId is { } requested && requested != Guid.Empty ? LanguageId.From(requested) : null),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}/manifest")]
    public async Task<ActionResult<GetContentPackManifestResult>> Manifest(
        Guid id,
        [FromServices] GetContentPackManifestPort getManifest,
        [FromQuery(Name = "language_id")] Guid? languageId,
        CancellationToken cancellationToken)
    {
        var tenant = User.GetLanguageId();
        var result = await getManifest.ExecuteAsync(
            new GetContentPackManifestCommand(
                tenant,
                ContentPackId.From(id),
                languageId is { } requested && requested != Guid.Empty ? LanguageId.From(requested) : null),
            cancellationToken);

        return Ok(result);
    }
}
