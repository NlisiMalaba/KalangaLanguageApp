using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = $"{nameof(Role.Contributor)},{nameof(Role.Admin)}")]
public sealed class CreateLessonUseCase(
    IUserRepository users,
    ILessonRepository lessons) : CreateLessonPort
{
    public async Task<CreateLessonResult> ExecuteAsync(
        CreateLessonCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        LessonAuthoringGuard.EnsureCanAuthor(actor, "create a lesson");

        var lesson = Lesson.CreateDraft(
            command.LanguageId,
            actor.Id,
            command.Title,
            command.Level,
            command.Category,
            DateTimeOffset.UtcNow,
            command.IsScenario,
            command.ScenarioContext,
            command.XpReward ?? DomainRules.DefaultLessonXpReward);

        await lessons.AddAsync(command.LanguageId, lesson, cancellationToken);

        return new CreateLessonResult(
            lesson.Id,
            lesson.LanguageId,
            lesson.Title,
            lesson.Level,
            lesson.Category,
            lesson.IsScenario,
            lesson.ScenarioContext,
            lesson.Status,
            lesson.ContributorId,
            lesson.XpReward);
    }
}
