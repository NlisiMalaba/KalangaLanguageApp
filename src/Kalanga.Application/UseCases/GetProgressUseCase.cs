using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class GetProgressUseCase(
    IUserRepository users,
    ILessonRepository lessons,
    ILearnerProgressRepository progress,
    IGamificationRepository gamification) : GetProgressPort
{
    public async Task<GetProgressResult> ExecuteAsync(
        GetProgressCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = await users.FindByIdAsync(command.LanguageId, command.ActorUserId, cancellationToken)
            ?? throw new UserNotFoundException(command.ActorUserId);

        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var published = await lessons.FindPublishedSummariesAsync(command.LanguageId, cancellationToken);
        var learnerProgress = await progress.FindByUserAsync(command.LanguageId, actor.Id, cancellationToken);
        var completedByLesson = learnerProgress
            .Where(static item => item.IsCompleted)
            .ToDictionary(static item => item.LessonId);

        var byLevel = published
            .GroupBy(static lesson => lesson.Level)
            .OrderBy(static group => group.Key)
            .Select(group => ToBucket(group.Key, category: null, group, completedByLesson))
            .ToList();

        var byCategory = published
            .GroupBy(static lesson => lesson.Category, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .Select(group => ToBucket(level: null, group.Key, group, completedByLesson))
            .ToList();

        var weakAreas = published
            .GroupBy(static lesson => (lesson.Level, lesson.Category))
            .Select(group => ToWeakArea(group.Key.Level, group.Key.Category, group, completedByLesson))
            .Where(static area => area is not null)
            .Select(static area => area!)
            .OrderBy(static area => area.AverageScore)
            .ThenBy(static area => area.Level)
            .ThenBy(static area => area.Category, StringComparer.Ordinal)
            .ToList();

        var totals = await gamification.FindByUserAsync(command.LanguageId, actor.Id, cancellationToken);

        return new GetProgressResult(actor.Id, totals?.TotalXp ?? 0, byLevel, byCategory, weakAreas);
    }

    private static ProgressBucketDto ToBucket(
        Level? level,
        string? category,
        IEnumerable<PublishedLessonSummary> lessons,
        IReadOnlyDictionary<LessonId, LearnerProgress> completedByLesson)
    {
        var catalog = lessons.ToList();
        var completed = catalog.Count(item => completedByLesson.ContainsKey(item.LessonId));
        var total = catalog.Count;
        var percentage = total == 0
            ? 0m
            : Math.Round(100m * completed / total, 1, MidpointRounding.AwayFromZero);

        return new ProgressBucketDto(level, category, completed, total, percentage);
    }

    private static WeakAreaDto? ToWeakArea(
        Level level,
        string category,
        IEnumerable<PublishedLessonSummary> lessons,
        IReadOnlyDictionary<LessonId, LearnerProgress> completedByLesson)
    {
        var scores = lessons
            .Select(lesson => completedByLesson.GetValueOrDefault(lesson.LessonId))
            .Where(static item => item is { Score: not null })
            .Select(static item => item!.Score!.Value)
            .ToList();

        if (scores.Count == 0)
        {
            return null;
        }

        var average = Math.Round((decimal)scores.Average(), 1, MidpointRounding.AwayFromZero);
        if (average >= DomainRules.WeakAreaScoreThreshold)
        {
            return null;
        }

        return new WeakAreaDto(level, category, average, scores.Count);
    }
}
