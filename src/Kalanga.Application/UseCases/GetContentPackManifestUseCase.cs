using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class GetContentPackManifestUseCase(
    IContentPackRepository packs,
    ILessonRepository lessons,
    IPhraseRepository phrases,
    ILanguageVariationRepository variations,
    IAudioRecordingRepository audio,
    IContentPackManifestSigner signer,
    TimeProvider? time = null) : GetContentPackManifestPort
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public async Task<GetContentPackManifestResult> ExecuteAsync(
        GetContentPackManifestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ListContentPacksUseCase.EnsureTenant(command.LanguageId, command.RequestedLanguageId);

        var pack = await packs.FindByIdAsync(command.LanguageId, command.PackId, cancellationToken)
            ?? throw new ContentPackNotFoundException(command.PackId);

        var publishedLessons = (await lessons.FindByIdsAsync(command.LanguageId, pack.LessonIds, cancellationToken))
            .Where(static lesson => lesson.IsVisibleInCatalog)
            .OrderBy(static lesson => lesson.Id.Value)
            .ToList();

        var lessonIds = publishedLessons.Select(static lesson => lesson.Id).ToArray();
        var lessonPhrases = await phrases.FindByLessonIdsAsync(command.LanguageId, lessonIds, cancellationToken);
        var phraseIds = lessonPhrases.Select(static phrase => phrase.Id).ToArray();
        var lessonVariations = await variations.FindByPhraseIdsAsync(command.LanguageId, phraseIds, cancellationToken);
        var variationIds = lessonVariations.Select(static variation => variation.Id).ToArray();

        var phraseAudio = await audio.FindByPhraseIdsAsync(command.LanguageId, phraseIds, cancellationToken);
        var variationAudio = await audio.FindByVariationIdsAsync(command.LanguageId, variationIds, cancellationToken);

        var phrasesByLesson = lessonPhrases
            .GroupBy(static phrase => phrase.LessonId)
            .ToDictionary(static group => group.Key, static group => group.ToList());
        var variationsByPhrase = lessonVariations
            .GroupBy(static variation => variation.PhraseId)
            .ToDictionary(static group => group.Key, static group => group.ToList());

        var playable = new List<AudioRecording>(phraseAudio.Count + variationAudio.Count);
        playable.AddRange(phraseAudio.Where(static recording => recording.IsPlayableByLearners));
        playable.AddRange(variationAudio.Where(static recording => recording.IsPlayableByLearners));

        var lessonDtos = publishedLessons
            .Select(lesson =>
            {
                var audioForLesson = CollectPlayableAudio(lesson.Id, phrasesByLesson, variationsByPhrase, playable);
                return new ContentPackManifestLessonDto(lesson.Id, audioForLesson);
            })
            .ToList();

        var sizeBytes = lessonDtos.Sum(static lesson => lesson.Audio.Sum(static item => (long)item.FileSizeBytes));
        var unsigned = new GetContentPackManifestResult(
            pack.Id,
            pack.LanguageId,
            pack.Name,
            pack.Version,
            pack.Level,
            pack.Category,
            _time.GetUtcNow(),
            sizeBytes,
            lessonDtos,
            Signature: string.Empty,
            signer.Algorithm);

        var signature = signer.Sign(ContentPackManifestCanonical.Write(unsigned));
        return unsigned with { Signature = signature };
    }

    private static IReadOnlyList<ContentPackManifestAudioDto> CollectPlayableAudio(
        LessonId lessonId,
        IReadOnlyDictionary<LessonId, List<Phrase>> phrasesByLesson,
        IReadOnlyDictionary<PhraseId, List<LanguageVariation>> variationsByPhrase,
        IReadOnlyList<AudioRecording> playable)
    {
        phrasesByLesson.TryGetValue(lessonId, out var lessonPhrases);
        var phraseIdSet = (lessonPhrases ?? []).Select(static phrase => phrase.Id).ToHashSet();
        var variationIdSet = new HashSet<LanguageVariationId>();
        foreach (var phrase in lessonPhrases ?? [])
        {
            if (variationsByPhrase.TryGetValue(phrase.Id, out var phraseVariations))
            {
                foreach (var variation in phraseVariations)
                {
                    variationIdSet.Add(variation.Id);
                }
            }
        }

        return playable
            .Where(recording =>
                (recording.VariationId is { } variationId && variationIdSet.Contains(variationId))
                || (recording.VariationId is null
                    && recording.PhraseId is { } phraseId
                    && phraseIdSet.Contains(phraseId)))
            .DistinctBy(static recording => recording.Id)
            .OrderBy(static recording => recording.Id.Value)
            .Select(static recording => new ContentPackManifestAudioDto(
                recording.Id,
                recording.CdnUrl,
                recording.FileFormat,
                recording.FileSizeBytes))
            .ToList();
    }
}
