using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application.Ports.Out;

public sealed record PublishedLessonSummary(LessonId LessonId, Level Level, string Category);
