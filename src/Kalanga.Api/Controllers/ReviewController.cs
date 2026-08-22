using Kalanga.Api.Authorization;
using Kalanga.Api.Contracts.Review;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.ReviewerOrAdmin)]
[Route("review")]
public sealed class ReviewController : ControllerBase
{
    [HttpGet("queue")]
    public async Task<ActionResult<ListReviewQueueResult>> Queue(
        [FromServices] ListReviewQueuePort listReviewQueue,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await listReviewQueue.ExecuteAsync(
            new ListReviewQueueCommand(User.GetLanguageId(), User.GetUserId(), skip, take),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ReviewLessonResult>> Approve(
        Guid id,
        [FromServices] ReviewLessonPort reviewLesson,
        CancellationToken cancellationToken)
    {
        return Ok(await reviewLesson.ExecuteAsync(
            new ReviewLessonCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                LessonId.From(id),
                ReviewLessonAction.Approve,
                Feedback: null),
            cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ReviewLessonResult>> Reject(
        Guid id,
        [FromBody] ReviewFeedbackRequest request,
        [FromServices] ReviewLessonPort reviewLesson,
        CancellationToken cancellationToken)
    {
        return Ok(await reviewLesson.ExecuteAsync(
            new ReviewLessonCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                LessonId.From(id),
                ReviewLessonAction.Reject,
                request.Feedback),
            cancellationToken));
    }

    [HttpPost("{id:guid}/request-revision")]
    public async Task<ActionResult<ReviewLessonResult>> RequestRevision(
        Guid id,
        [FromBody] ReviewFeedbackRequest request,
        [FromServices] ReviewLessonPort reviewLesson,
        CancellationToken cancellationToken)
    {
        return Ok(await reviewLesson.ExecuteAsync(
            new ReviewLessonCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                LessonId.From(id),
                ReviewLessonAction.RequestRevision,
                request.Feedback),
            cancellationToken));
    }
}
