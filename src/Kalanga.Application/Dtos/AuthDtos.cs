using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public sealed record RegisterUserCommand(
    LanguageId LanguageId,
    string Email,
    string Password,
    string DisplayName);

public sealed record RegisterUserResult(
    UserId UserId,
    LanguageId LanguageId,
    string Email,
    string DisplayName,
    Role Role);

public sealed record AuthenticateUserCommand(
    LanguageId LanguageId,
    string Email,
    string Password);

public sealed record AuthenticateUserResult(
    UserId UserId,
    LanguageId LanguageId,
    string Email,
    string DisplayName,
    Role Role,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);
