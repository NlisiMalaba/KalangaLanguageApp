using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[AllowAnonymous]
public sealed class RegisterUserUseCase(
    IUserRepository users,
    IPasswordHasher passwordHasher) : RegisterUserPort
{
    public async Task<RegisterUserResult> ExecuteAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await users.FindByEmailAsync(command.LanguageId, command.Email, cancellationToken);
        if (existing is not null)
        {
            throw new DuplicateEmailException(command.Email);
        }

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = User.Register(
            command.LanguageId,
            command.Email,
            passwordHash,
            command.DisplayName,
            DateTimeOffset.UtcNow);

        await users.AddAsync(command.LanguageId, user, cancellationToken);

        return new RegisterUserResult(
            user.Id,
            user.LanguageId,
            user.Email,
            user.DisplayName,
            user.Role);
    }
}
