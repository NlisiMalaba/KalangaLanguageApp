import { CatalogApiError } from '@/domain/catalog/errors';
import type {
  AudioRef,
  LessonDetail,
  LessonExerciseDetail,
  LessonPhraseDetail,
  LessonVariationDetail,
} from '@/domain/catalog/types';
import { AudioFileFormat, ExerciseType, SpeakerGender } from '@/domain/enums';
import { mapLevel, requireNumber, requireString, unwrapId, type IdWire } from '@/lib/catalog/wire';

type AudioWire = {
  audioRecordingId?: IdWire;
  cdnUrl?: unknown;
  fileFormat?: unknown;
  speakerGender?: unknown;
  dialectLabel?: unknown;
  durationMs?: unknown;
};

type VariationWire = {
  variationId?: IdWire;
  kalangaText?: unknown;
  registerLabel?: unknown;
  audio?: AudioWire[];
};

type PhraseWire = {
  phraseId?: IdWire;
  kalangaText?: unknown;
  englishTranslation?: unknown;
  sortOrder?: unknown;
  variations?: VariationWire[];
  audio?: AudioWire[];
};

type ExerciseWire = {
  exerciseId?: IdWire;
  exerciseType?: unknown;
  promptData?: unknown;
  correctAnswer?: unknown;
  sortOrder?: unknown;
};

type LessonWire = {
  lessonId?: IdWire;
  languageId?: IdWire;
  title?: unknown;
  level?: unknown;
  category?: unknown;
  isScenario?: unknown;
  scenarioContext?: unknown;
  xpReward?: unknown;
  updatedAt?: unknown;
  phrases?: PhraseWire[];
  exercises?: ExerciseWire[];
};

const EXERCISE_BY_NUMBER = [
  ExerciseType.Flashcard,
  ExerciseType.MultipleChoice,
  ExerciseType.SentenceBuilder,
  ExerciseType.Listening,
];

const FORMAT_BY_NUMBER = [AudioFileFormat.Mp3, AudioFileFormat.Aac];
const GENDER_BY_NUMBER = [SpeakerGender.Unspecified, SpeakerGender.Male, SpeakerGender.Female];

function mapNamedEnum<T extends string>(
  value: unknown,
  values: readonly T[],
  byNumber: readonly T[],
  field: string,
): T {
  if (typeof value === 'string' && (values as readonly string[]).includes(value)) {
    return value as T;
  }

  if (typeof value === 'number' && byNumber[value]) {
    return byNumber[value];
  }

  throw new CatalogApiError(`Catalog response has an invalid ${field}.`);
}

function mapAudio(items: AudioWire[] | undefined): AudioRef[] {
  return (items ?? []).map((item) => ({
    id: unwrapId(item.audioRecordingId, 'audioRecordingId'),
    cdnUrl: requireString(item.cdnUrl, 'cdnUrl'),
    fileFormat: mapNamedEnum(item.fileFormat, Object.values(AudioFileFormat), FORMAT_BY_NUMBER, 'fileFormat'),
    speakerGender: mapNamedEnum(
      item.speakerGender,
      Object.values(SpeakerGender),
      GENDER_BY_NUMBER,
      'speakerGender',
    ),
    dialectLabel: typeof item.dialectLabel === 'string' ? item.dialectLabel : null,
    durationMs: requireNumber(item.durationMs, 'durationMs'),
  }));
}

function mapVariation(item: VariationWire): LessonVariationDetail {
  return {
    id: unwrapId(item.variationId, 'variationId'),
    kalangaText: requireString(item.kalangaText, 'kalangaText'),
    registerLabel: requireString(item.registerLabel, 'registerLabel'),
    audio: mapAudio(item.audio),
  };
}

function mapPhrase(item: PhraseWire): LessonPhraseDetail {
  return {
    id: unwrapId(item.phraseId, 'phraseId'),
    kalangaText: requireString(item.kalangaText, 'kalangaText'),
    englishTranslation: requireString(item.englishTranslation, 'englishTranslation'),
    sortOrder: requireNumber(item.sortOrder, 'sortOrder'),
    variations: (item.variations ?? []).map(mapVariation),
    audio: mapAudio(item.audio),
  };
}

function mapExercise(item: ExerciseWire): LessonExerciseDetail {
  return {
    id: unwrapId(item.exerciseId, 'exerciseId'),
    exerciseType: mapNamedEnum(
      item.exerciseType,
      Object.values(ExerciseType),
      EXERCISE_BY_NUMBER,
      'exerciseType',
    ),
    promptData: requireString(item.promptData, 'promptData'),
    correctAnswer: requireString(item.correctAnswer, 'correctAnswer'),
    sortOrder: requireNumber(item.sortOrder, 'sortOrder'),
  };
}

export function mapGetLessonResponse(body: unknown): LessonDetail {
  const lesson = (body as { lesson?: LessonWire } | null)?.lesson;
  if (!lesson) {
    throw new CatalogApiError('Catalog response is missing lesson.');
  }

  const phrases = [...(lesson.phrases ?? []).map(mapPhrase)].sort((a, b) => a.sortOrder - b.sortOrder);
  const exercises = [...(lesson.exercises ?? []).map(mapExercise)].sort((a, b) => a.sortOrder - b.sortOrder);

  return {
    id: unwrapId(lesson.lessonId, 'lessonId'),
    languageId: unwrapId(lesson.languageId, 'languageId'),
    title: requireString(lesson.title, 'title'),
    level: mapLevel(lesson.level),
    category: requireString(lesson.category, 'category'),
    isScenario: lesson.isScenario === true,
    scenarioContext: typeof lesson.scenarioContext === 'string' ? lesson.scenarioContext : null,
    xpReward: requireNumber(lesson.xpReward, 'xpReward'),
    updatedAt: requireString(lesson.updatedAt, 'updatedAt'),
    phrases,
    exercises,
  };
}
