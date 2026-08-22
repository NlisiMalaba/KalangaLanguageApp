using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence.Repositories;

internal sealed class AudioRecordingRepository(KalangaDbContext db) : IAudioRecordingRepository
{
    public async Task<AudioRecording?> FindByIdAsync(
        LanguageId languageId,
        AudioRecordingId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.AudioRecordings
            .AsNoTracking()
            .FirstOrDefaultAsync(audio => audio.LanguageId == languageId.Value && audio.Id == id.Value, cancellationToken);
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<AudioRecording>> FindByPhraseIdAsync(
        LanguageId languageId,
        PhraseId phraseId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.AudioRecordings
            .AsNoTracking()
            .Where(audio => audio.LanguageId == languageId.Value && audio.PhraseId == phraseId.Value)
            .OrderBy(audio => audio.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task<IReadOnlyList<AudioRecording>> FindByPhraseIdsAsync(
        LanguageId languageId,
        IReadOnlyCollection<PhraseId> phraseIds,
        CancellationToken cancellationToken = default)
    {
        if (phraseIds.Count == 0)
        {
            return [];
        }

        var ids = phraseIds.Select(static id => id.Value).ToArray();
        var records = await db.AudioRecordings
            .AsNoTracking()
            .Where(audio => audio.LanguageId == languageId.Value && audio.PhraseId != null && ids.Contains(audio.PhraseId.Value))
            .OrderBy(audio => audio.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task<IReadOnlyList<AudioRecording>> FindByVariationIdsAsync(
        LanguageId languageId,
        IReadOnlyCollection<LanguageVariationId> variationIds,
        CancellationToken cancellationToken = default)
    {
        if (variationIds.Count == 0)
        {
            return [];
        }

        var ids = variationIds.Select(static id => id.Value).ToArray();
        var records = await db.AudioRecordings
            .AsNoTracking()
            .Where(audio => audio.LanguageId == languageId.Value && audio.VariationId != null && ids.Contains(audio.VariationId.Value))
            .OrderBy(audio => audio.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task<IReadOnlyList<AudioRecording>> FindPendingReviewAsync(
        LanguageId languageId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        TenantGuard.EnsureSkip(skip);
        take = TenantGuard.ClampTake(take);
        var pending = AudioRecordingStatus.PendingReview.ToString();

        var records = await db.AudioRecordings
            .AsNoTracking()
            .Where(audio => audio.LanguageId == languageId.Value && audio.Status == pending)
            .OrderBy(audio => audio.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return records.ConvertAll(static record => record.ToDomain());
    }

    public async Task AddAsync(LanguageId languageId, AudioRecording recording, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, recording.LanguageId);
        db.AudioRecordings.Add(recording.ToRecord());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LanguageId languageId, AudioRecording recording, CancellationToken cancellationToken = default)
    {
        TenantGuard.Ensure(languageId, recording.LanguageId);
        var record = await db.AudioRecordings.FirstOrDefaultAsync(
            row => row.LanguageId == languageId.Value && row.Id == recording.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"Audio recording {recording.Id} was not found for this language.");
        recording.CopyTo(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
