using Kalanga.Api.Authorization;
using Kalanga.Api.Contracts.Requests;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[Authorize]
[Route("requests")]
public sealed class RequestsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ListRequestsResult>> List(
        [FromServices] ListRequestsPort listRequests,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await listRequests.ExecuteAsync(
            new ListRequestsCommand(User.GetLanguageId(), User.GetUserId(), skip, take),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SubmitRequestResult>> Submit(
        [FromBody] SubmitRequestBody request,
        [FromServices] SubmitRequestPort submitRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await submitRequest.ExecuteAsync(
            new SubmitRequestCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                request.Title,
                request.Description),
            cancellationToken);

        return Created($"/requests/{result.Request.RequestId.Value}", result);
    }

    [HttpPost("{id:guid}/upvote")]
    public async Task<ActionResult<UpvoteRequestResult>> Upvote(
        Guid id,
        [FromServices] UpvoteRequestPort upvoteRequest,
        CancellationToken cancellationToken)
    {
        var result = await upvoteRequest.ExecuteAsync(
            new UpvoteRequestCommand(User.GetLanguageId(), User.GetUserId(), RequestId.From(id)),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/fulfill")]
    [Authorize(Policy = AuthPolicies.ContributorOrAdmin)]
    public async Task<ActionResult<FulfillRequestResult>> Fulfill(
        Guid id,
        [FromBody] FulfillRequestBody body,
        [FromServices] FulfillRequestPort fulfillRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        var result = await fulfillRequest.ExecuteAsync(
            new FulfillRequestCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                RequestId.From(id),
                LessonId.From(body.LessonId)),
            cancellationToken);

        return Ok(result);
    }
}
