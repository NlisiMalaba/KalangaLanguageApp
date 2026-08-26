import type { AudioRef, LessonDetail } from '@/domain/catalog/types';
import type { AudioRecording, EntityId } from '@/domain/entities';
import { LessonStatus } from '@/domain/enums';
import { listApprovedAudioForPhrases, listApprovedAudioForVariations } from '@/lib/audioRecordings';
import { listExercisesForLesson } from '@/lib/exercises';
import { listVariationsForPhrases } from '@/lib/languageVariations';
import { getLesson } from '@/lib/lessons';
import type { LocalStore } from '@/lib/localStore';
import { listPhrasesForLesson } from '@/lib/phrases';

function toAudioRef(recording: AudioRecording): AudioRef {
  return {
    id: recording.id,
    cdnUrl: recording.cdnUrl,
    fileFormat: recording.fileFormat,
    speakerGender: recording.speakerGender,
    dialectLabel: recording.dialectLabel,
    durationMs: recording.durationMs,
  };
}

export async function loadLocalLessonDetail(
  languageId: EntityId,
  lessonId: EntityId,
  store?: LocalStore,
): Promise<LessonDetail | null> {
  const lesson = await getLesson(languageId, lessonId, store);
  if (!lesson || lesson.status !== LessonStatus.Published) {
    return null;
  }

  const phrases = await listPhrasesForLesson(languageId, lessonId, store);
  const phraseIds = phrases.map((phrase) => phrase.id);
  const variations = await listVariationsForPhrases(languageId, phraseIds, store);
  const variationIds = variations.map((variation) => variation.id);
  const [phraseAudio, variationAudio, exercises] = await Promise.all([
    listApprovedAudioForPhrases(languageId, phraseIds, store),
    listApprovedAudioForVariations(languageId, variationIds, store),
    listExercisesForLesson(languageId, lessonId, store),
  ]);

  const audioByPhrase = new Map<EntityId, AudioRef[]>();
  for (const recording of phraseAudio) {
    if (!recording.phraseId) {
      continue;
    }

    const bucket = audioByPhrase.get(recording.phraseId) ?? [];
    bucket.push(toAudioRef(recording));
    audioByPhrase.set(recording.phraseId, bucket);
  }

  const audioByVariation = new Map<EntityId, AudioRef[]>();
  for (const recording of variationAudio) {
    if (!recording.variationId) {
      continue;
    }

    const bucket = audioByVariation.get(recording.variationId) ?? [];
    bucket.push(toAudioRef(recording));
    audioByVariation.set(recording.variationId, bucket);
  }

  const variationsByPhrase = new Map<EntityId, typeof variations>();
  for (const variation of variations) {
    const bucket = variationsByPhrase.get(variation.phraseId) ?? [];
    bucket.push(variation);
    variationsByPhrase.set(variation.phraseId, bucket);
  }

  return {
    id: lesson.id,
    languageId: lesson.languageId,
    title: lesson.title,
    level: lesson.level,
    category: lesson.category,
    isScenario: lesson.isScenario,
    scenarioContext: lesson.scenarioContext,
    xpReward: lesson.xpReward,
    updatedAt: lesson.updatedAt,
    phrases: phrases.map((phrase) => ({
      id: phrase.id,
      kalangaText: phrase.kalangaText,
      englishTranslation: phrase.englishTranslation,
      sortOrder: phrase.sortOrder,
      variations: (variationsByPhrase.get(phrase.id) ?? []).map((variation) => ({
        id: variation.id,
        kalangaText: variation.kalangaText,
        registerLabel: variation.registerLabel,
        audio: audioByVariation.get(variation.id) ?? [],
      })),
      audio: audioByPhrase.get(phrase.id) ?? [],
    })),
    exercises: exercises.map((exercise) => ({
      id: exercise.id,
      exerciseType: exercise.exerciseType,
      promptData: exercise.promptData,
      correctAnswer: exercise.correctAnswer,
      sortOrder: exercise.sortOrder,
    })),
  };
}
