using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public sealed record RefreshTokenCommand(LanguageId LanguageId, string RefreshToken);

public sealed record LogoutCommand(LanguageId LanguageId, string RefreshToken);

public sealed record ListUsersCommand(LanguageId LanguageId, UserId ActorUserId, int Skip, int Take);

public sealed record AdminUserDto(
    UserId UserId,
    LanguageId LanguageId,
    string Email,
    string DisplayName,
    Role Role,
    UserStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ListUsersResult(IReadOnlyList<AdminUserDto> Users);

public sealed record GetPlatformMetricsCommand(LanguageId LanguageId, UserId ActorUserId);

public sealed record PlatformMetricsResult(
    int TotalUsers,
    int TotalPublishedLessons,
    int TotalApprovedAudioRecordings);
