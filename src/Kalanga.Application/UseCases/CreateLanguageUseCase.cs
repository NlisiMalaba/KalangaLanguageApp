using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Kalanga.Application.UseCases;

[Authorize(Roles = nameof(Role.Admin))]
public sealed class CreateLanguageUseCase(
    IUserRepository users,
    ILanguageRepository languages,
    ILogger<CreateLanguageUseCase> logger) : CreateLanguagePort
{
    public async Task<CreateLanguageResult> ExecuteAsync(
        CreateLanguageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.ActorLanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.Role != Role.Admin)
        {
            throw new UnauthorizedRoleException("create languages", Role.Admin);
        }

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var language = Language.Create(command.Code, command.Name, command.Region, DateTimeOffset.UtcNow);
        await languages.AddAsync(language, cancellationToken);

        logger.LogInformation(
            "Language created. Code {LanguageCode} LanguageId {LanguageId} ActorUserId {ActorUserId}",
            language.Code,
            language.Id.Value,
            actor.Id.Value);

        return new CreateLanguageResult(language.ToDto());
    }
}
