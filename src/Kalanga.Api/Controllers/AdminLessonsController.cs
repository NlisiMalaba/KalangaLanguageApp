using Kalanga.Api.Authorization;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Route("admin/lessons")]
public sealed class AdminLessonsController : ControllerBase
{
    [HttpPut("{id:guid}/publish")]
    public async Task<ActionResult<OverrideLessonPublicationResult>> Publish(
        Guid id,
        [FromServices] OverrideLessonPublicationPort overridePublication,
        CancellationToken cancellationToken)
    {
        return Ok(await overridePublication.ExecuteAsync(
            new OverrideLessonPublicationCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                LessonId.From(id),
                AdminLessonOverrideAction.Publish),
            cancellationToken));
    }

    [HttpPut("{id:guid}/unpublish")]
    public async Task<ActionResult<OverrideLessonPublicationResult>> Unpublish(
        Guid id,
        [FromServices] OverrideLessonPublicationPort overridePublication,
        CancellationToken cancellationToken)
    {
        return Ok(await overridePublication.ExecuteAsync(
            new OverrideLessonPublicationCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                LessonId.From(id),
                AdminLessonOverrideAction.Unpublish),
            cancellationToken));
    }
}
