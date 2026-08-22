using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class LessonRepository(KalangaDbContext db) : ILessonRepository
{
    public async Task<Lesson?> FindByIdAsync(LanguageId languageId, LessonId id, CancellationToken cancellationToken = default)
    {
        var record = await db.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(lesson => lesson.LanguageId == languageId.Value && lesson.Id == id.Value, cancellationToken);
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Lesson>> FindByIdsAsync(
        LanguageId languageId,
        IReadOnlyCollection<LessonId> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var values = ids.Select(static id => id.Value).Distinct().ToArray();
        var records = await db.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.LanguageId == languageId.Value && values.Contains(lesson.Id))
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task<IReadOnlyList<Lesson>> FindPublishedAsync(
        LanguageId languageId,
        Level? level,
        string? category,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        TenantGuard.EnsureSkip(skip);
        take = TenantGuard.ClampTake(take);

        var published = LessonStatus.Published.ToString();
        var query = db.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.LanguageId == languageId.Value && lesson.Status == published);

        if (level is { } selectedLevel)
        {
            var storedLevel = selectedLevel.ToString();
            query = query.Where(lesson => lesson.Level == storedLevel);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(lesson => lesson.Category == category);
        }

        var records = await query
            .OrderBy(lesson => lesson.Title)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task<IReadOnlyList<Lesson>> FindPendingReviewAsync(
        LanguageId languageId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        TenantGuard.EnsureSkip(skip);
        take = TenantGuard.ClampTake(take);
        var pending = LessonStatus.PendingReview.ToString();

        var records = await db.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.LanguageId == languageId.Value && lesson.Status == pending)
            .OrderBy(lesson => lesson.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task<IReadOnlyList<Lesson>> FindByContributorAsync(
        LanguageId languageId,
        UserId contributorId,
        LessonStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = db.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.LanguageId == languageId.Value && lesson.ContributorId == contributorId.Value);

        if (status is { } selectedStatus)
        {
            var stored = selectedStatus.ToString();
            query = query.Where(lesson => lesson.Status == stored);
        }

        var records = await query
            .OrderByDescending(lesson => lesson.UpdatedAt)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task AddAsync(LanguageId languageId, Lesson lesson, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, lesson.LanguageId);
        db.Lessons.Add(lesson.ToRecord());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LanguageId languageId, Lesson lesson, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, lesson.LanguageId);
        var record = await db.Lessons.FirstOrDefaultAsync(
            row => row.LanguageId == languageId.Value && row.Id == lesson.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"Lesson {lesson.Id} was not found for this language.");
        lesson.CopyTo(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
