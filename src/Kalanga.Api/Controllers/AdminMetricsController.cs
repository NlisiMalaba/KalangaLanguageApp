using Kalanga.Api.Authorization;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Route("admin/metrics")]
public sealed class AdminMetricsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformMetricsResult>> Get(
        [FromServices] GetPlatformMetricsPort getPlatformMetrics,
        CancellationToken cancellationToken)
    {
        var result = await getPlatformMetrics.ExecuteAsync(
            new GetPlatformMetricsCommand(User.GetLanguageId(), User.GetUserId()),
            cancellationToken);

        return Ok(result);
    }
}
