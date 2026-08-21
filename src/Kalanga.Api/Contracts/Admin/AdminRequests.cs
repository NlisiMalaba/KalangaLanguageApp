using Kalanga.Domain.Enums;

namespace Kalanga.Api.Contracts.Admin;

public sealed record ChangeUserRoleRequest(Role Role);

public sealed record ChangeUserStatusRequest(UserStatus Status);
