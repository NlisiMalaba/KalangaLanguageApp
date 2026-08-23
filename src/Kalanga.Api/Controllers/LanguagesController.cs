using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("languages")]
public sealed class LanguagesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ListLanguagesResult>> List(
        [FromServices] ListLanguagesPort listLanguages,
        CancellationToken cancellationToken)
    {
        return Ok(await listLanguages.ExecuteAsync(cancellationToken));
    }
}
