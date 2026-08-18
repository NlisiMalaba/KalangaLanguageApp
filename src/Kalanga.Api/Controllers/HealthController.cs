using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => await Task.FromResult(Ok(new { status = "OK" }));
}
