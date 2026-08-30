using Kalanga.Api.Authorization;
using Kalanga.Api.Contracts.Admin;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Route("admin/languages")]
public sealed class AdminLanguagesController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateLanguageResult>> Create(
        [FromBody] CreateLanguageRequest request,
        [FromServices] CreateLanguagePort createLanguage,
        CancellationToken cancellationToken)
    {
        var result = await createLanguage.ExecuteAsync(
            new CreateLanguageCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                request.Code,
                request.Name,
                request.Region),
            cancellationToken);

        return Created($"/languages", result);
    }
}
