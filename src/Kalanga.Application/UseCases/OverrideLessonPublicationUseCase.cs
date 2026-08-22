using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = nameof(Role.Admin))]
public sealed class OverrideLessonPublicationUseCase(
    IUserRepository users,
    ILessonRepository lessons,
    ICatalogCache? catalogCache = null) : OverrideLessonPublicationPort
{
    public async Task<OverrideLessonPublicationResult> ExecuteAsync(
        OverrideLessonPublicationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        LessonReviewGuard.EnsureAdmin(actor, "override lesson publication");

        var lesson = await lessons.FindByIdAsync(command.LanguageId, command.LessonId, cancellationToken)
            ?? throw new LessonNotFoundException(command.LessonId);

        var utcNow = DateTimeOffset.UtcNow;
        switch (command.Action)
        {
            case AdminLessonOverrideAction.Publish:
                lesson.Publish(utcNow);
                break;

            case AdminLessonOverrideAction.Unpublish:
                lesson.Unpublish(utcNow);
                break;

            default:
                throw new InvalidReviewException($"Unsupported publication override '{command.Action}'.");
        }

        await lessons.UpdateAsync(command.LanguageId, lesson, cancellationToken);
        catalogCache?.Invalidate(command.LanguageId);

        return new OverrideLessonPublicationResult(lesson.Id, lesson.LanguageId, lesson.Status);
    }
}
