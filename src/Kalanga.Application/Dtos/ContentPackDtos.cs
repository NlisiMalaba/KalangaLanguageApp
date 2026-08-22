using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Dtos;

public sealed record ListContentPacksCommand(
    LanguageId LanguageId,
    Level? Level,
    string? Category,
    LanguageId? RequestedLanguageId = null);

public sealed record ContentPackListItemDto(
    ContentPackId PackId,
    LanguageId LanguageId,
    string Name,
    Level? Level,
    string? Category,
    int Version,
    long SizeBytes,
    string ManifestUrl);

public sealed record ListContentPacksResult(IReadOnlyList<ContentPackListItemDto> Packs);

public sealed record GetContentPackManifestCommand(
    LanguageId LanguageId,
    ContentPackId PackId,
    LanguageId? RequestedLanguageId = null);

public sealed record ContentPackManifestAudioDto(
    AudioRecordingId AudioRecordingId,
    string CdnUrl,
    AudioFileFormat FileFormat,
    int FileSizeBytes);

public sealed record ContentPackManifestLessonDto(
    LessonId LessonId,
    IReadOnlyList<ContentPackManifestAudioDto> Audio);

public sealed record GetContentPackManifestResult(
    ContentPackId PackId,
    LanguageId LanguageId,
    string Name,
    int Version,
    Level? Level,
    string? Category,
    DateTimeOffset GeneratedAt,
    long SizeBytes,
    IReadOnlyList<ContentPackManifestLessonDto> Lessons,
    string Signature,
    string Algorithm);
