using Kalanga.Domain.Enums;

namespace Kalanga.Api.Contracts.Admin;

public sealed record CreateLanguageRequest(string Code, string Name, string Region);

public sealed record ChangeUserRoleRequest(Role Role);

public sealed record ChangeUserStatusRequest(UserStatus Status);
