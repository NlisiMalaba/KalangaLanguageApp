using Kalanga.Api.Authorization;
using Kalanga.Api.Contracts.Lessons;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Domain;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[Authorize]
[Route("lessons")]
public sealed class LessonsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BrowseLessonCatalogResult>> List(
        [FromServices] BrowseLessonCatalogPort browseCatalog,
        [FromQuery] Level? level,
        [FromQuery] string? category,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await browseCatalog.ExecuteAsync(
            new BrowseLessonCatalogCommand(User.GetLanguageId(), level, category, skip, take),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetLessonResult>> Get(
        Guid id,
        [FromServices] GetLessonPort getLesson,
        CancellationToken cancellationToken)
    {
        var result = await getLesson.ExecuteAsync(
            new GetLessonCommand(User.GetLanguageId(), LessonId.From(id)),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}/phrases")]
    public async Task<ActionResult<IReadOnlyList<PhraseDetailDto>>> Phrases(
        Guid id,
        [FromServices] GetLessonPort getLesson,
        CancellationToken cancellationToken)
    {
        var result = await getLesson.ExecuteAsync(
            new GetLessonCommand(User.GetLanguageId(), LessonId.From(id)),
            cancellationToken);

        return Ok(result.Lesson.Phrases);
    }

    [HttpGet("{id:guid}/exercises")]
    public async Task<ActionResult<IReadOnlyList<ExerciseDetailDto>>> Exercises(
        Guid id,
        [FromServices] GetLessonPort getLesson,
        CancellationToken cancellationToken)
    {
        var result = await getLesson.ExecuteAsync(
            new GetLessonCommand(User.GetLanguageId(), LessonId.From(id)),
            cancellationToken);

        return Ok(result.Lesson.Exercises);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ContributorOrAdmin)]
    public async Task<ActionResult<CreateLessonResult>> Create(
        [FromBody] CreateLessonRequest request,
        [FromServices] CreateLessonPort createLesson,
        CancellationToken cancellationToken)
    {
        var result = await createLesson.ExecuteAsync(
            new CreateLessonCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                request.Title,
                request.Level,
                request.Category,
                request.IsScenario,
                request.ScenarioContext,
                request.XpReward),
            cancellationToken);

        return Created($"/lessons/{result.LessonId.Value}", result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.ContributorOrAdmin)]
    public async Task<ActionResult<SaveLessonDraftResult>> Update(
        Guid id,
        [FromBody] UpdateLessonRequest request,
        [FromServices] SaveLessonDraftPort saveDraft,
        CancellationToken cancellationToken)
    {
        var result = await saveDraft.ExecuteAsync(
            new SaveLessonDraftCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                LessonId.From(id),
                request.Title,
                request.Level,
                request.Category,
                request.IsScenario,
                request.ScenarioContext,
                request.XpReward ?? DomainRules.DefaultLessonXpReward),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = AuthPolicies.ContributorOrAdmin)]
    public async Task<ActionResult<SubmitLessonForReviewResult>> Submit(
        Guid id,
        [FromServices] SubmitLessonForReviewPort submitLesson,
        CancellationToken cancellationToken)
    {
        var result = await submitLesson.ExecuteAsync(
            new SubmitLessonForReviewCommand(User.GetLanguageId(), User.GetUserId(), LessonId.From(id)),
            cancellationToken);

        return Ok(result);
    }
}
