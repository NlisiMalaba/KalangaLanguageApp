using Kalanga.Domain.Enums;

namespace Kalanga.Api.Contracts.Lessons;

public sealed record CreateLessonRequest(
    string Title,
    Level Level,
    string Category,
    bool IsScenario = false,
    string? ScenarioContext = null,
    int? XpReward = null);

public sealed record UpdateLessonRequest(
    string Title,
    Level Level,
    string Category,
    bool IsScenario = false,
    string? ScenarioContext = null,
    int? XpReward = null);
