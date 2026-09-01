using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Entities;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Application;

internal static class LessonDetailComposer
{
    public static async Task<LessonDetailDto> ComposeAsync(
        LanguageId languageId,
        Lesson lesson,
        IPhraseRepository phrases,
        ILanguageVariationRepository variations,
        IAudioRecordingRepository audio,
        IExerciseRepository exercises,
        CancellationToken cancellationToken,
        bool includeUnapprovedAudio = false)
    {
        var lessonPhrases = await phrases.FindByLessonIdAsync(languageId, lesson.Id, cancellationToken);
        var lessonExercises = await exercises.FindByLessonIdAsync(languageId, lesson.Id, cancellationToken);
        var phraseIds = lessonPhrases.Select(static phrase => phrase.Id).ToArray();

        var lessonVariations = await variations.FindByPhraseIdsAsync(languageId, phraseIds, cancellationToken);
        var variationIds = lessonVariations.Select(static variation => variation.Id).ToArray();

        var phraseAudio = await audio.FindByPhraseIdsAsync(languageId, phraseIds, cancellationToken);
        var variationAudio = await audio.FindByVariationIdsAsync(languageId, variationIds, cancellationToken);

        var audioByPhrase = IndexByPhrase(phraseAudio, includeUnapprovedAudio);
        var audioByVariation = IndexByVariation(variationAudio, includeUnapprovedAudio);
        var variationsByPhrase = lessonVariations
            .GroupBy(static variation => variation.PhraseId)
            .ToDictionary(static group => group.Key, static group => group.ToList());

        var phraseDtos = lessonPhrases
            .Select(phrase => MapPhrase(phrase, variationsByPhrase, audioByPhrase, audioByVariation))
            .ToList();

        var exerciseDtos = lessonExercises
            .Select(static exercise => new ExerciseDetailDto(
                exercise.Id,
                exercise.ExerciseType,
                exercise.PromptData,
                exercise.CorrectAnswer,
                exercise.SortOrder))
            .ToList();

        return new LessonDetailDto(
            lesson.Id,
            lesson.LanguageId,
            lesson.Title,
            lesson.Level,
            lesson.Category,
            lesson.IsScenario,
            lesson.ScenarioContext,
            lesson.XpReward,
            lesson.UpdatedAt,
            phraseDtos,
            exerciseDtos);
    }

    private static PhraseDetailDto MapPhrase(
        Phrase phrase,
        IReadOnlyDictionary<PhraseId, List<LanguageVariation>> variationsByPhrase,
        IReadOnlyDictionary<PhraseId, List<AudioRecording>> audioByPhrase,
        IReadOnlyDictionary<LanguageVariationId, List<AudioRecording>> audioByVariation)
    {
        variationsByPhrase.TryGetValue(phrase.Id, out var phraseVariations);
        audioByPhrase.TryGetValue(phrase.Id, out var phraseAudio);

        var variationDtos = (phraseVariations ?? [])
            .Select(variation =>
            {
                audioByVariation.TryGetValue(variation.Id, out var variationAudio);
                return new LanguageVariationDto(
                    variation.Id,
                    variation.KalangaText,
                    variation.RegisterLabel,
                    MapAudio(variationAudio));
            })
            .ToList();

        return new PhraseDetailDto(
            phrase.Id,
            phrase.KalangaText,
            phrase.EnglishTranslation,
            phrase.SortOrder,
            variationDtos,
            MapAudio(phraseAudio));
    }

    private static IReadOnlyList<AudioRefDto> MapAudio(IReadOnlyList<AudioRecording>? recordings) =>
        (recordings ?? [])
            .Select(static recording => new AudioRefDto(
                recording.Id,
                recording.CdnUrl,
                recording.FileFormat,
                recording.SpeakerGender,
                recording.DialectLabel,
                recording.DurationMs))
            .ToList();

    private static Dictionary<PhraseId, List<AudioRecording>> IndexByPhrase(
        IReadOnlyList<AudioRecording> recordings,
        bool includeUnapprovedAudio)
    {
        var indexed = new Dictionary<PhraseId, List<AudioRecording>>();
        foreach (var recording in recordings)
        {
            if ((!includeUnapprovedAudio && !recording.IsPlayableByLearners)
                || recording.PhraseId is not { } phraseId
                || recording.VariationId is not null)
            {
                continue;
            }

            if (!indexed.TryGetValue(phraseId, out var bucket))
            {
                bucket = [];
                indexed[phraseId] = bucket;
            }

            bucket.Add(recording);
        }

        return indexed;
    }

    private static Dictionary<LanguageVariationId, List<AudioRecording>> IndexByVariation(
        IReadOnlyList<AudioRecording> recordings,
        bool includeUnapprovedAudio)
    {
        var indexed = new Dictionary<LanguageVariationId, List<AudioRecording>>();
        foreach (var recording in recordings)
        {
            if ((!includeUnapprovedAudio && !recording.IsPlayableByLearners)
                || recording.VariationId is not { } variationId)
            {
                continue;
            }

            if (!indexed.TryGetValue(variationId, out var bucket))
            {
                bucket = [];
                indexed[variationId] = bucket;
            }

            bucket.Add(recording);
        }

        return indexed;
    }
}
