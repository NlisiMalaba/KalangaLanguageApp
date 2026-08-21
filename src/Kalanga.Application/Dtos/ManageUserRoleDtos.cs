using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public enum ManageUserAction
{
    ChangeRole = 0,
    Suspend = 1,
    Reactivate = 2,
}

public sealed record ManageUserRoleCommand(
    LanguageId LanguageId,
    UserId ActorUserId,
    UserId TargetUserId,
    ManageUserAction Action,
    Role? NewRole);

public sealed record ManageUserRoleResult(
    UserId UserId,
    LanguageId LanguageId,
    string Email,
    string DisplayName,
    Role Role,
    UserStatus Status);
