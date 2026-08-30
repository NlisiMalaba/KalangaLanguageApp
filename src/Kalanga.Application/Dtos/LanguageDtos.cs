using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public sealed record LanguageDto(
    LanguageId LanguageId,
    string Code,
    string Name,
    string Region,
    bool IsActive);

public sealed record CreateLanguageCommand(
    LanguageId ActorLanguageId,
    UserId ActorUserId,
    string Code,
    string Name,
    string Region);

public sealed record CreateLanguageResult(LanguageDto Language);

public sealed record ListLanguagesResult(IReadOnlyList<LanguageDto> Languages);
