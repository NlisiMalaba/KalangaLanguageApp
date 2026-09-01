import { AUDIO_UPLOAD_TIMEOUT_MS } from '@/constants/contributor';
import { AuthApiError } from '@/domain/auth/errors';
import { ContributorApiError } from '@/domain/contributor/errors';
import { encodeExercisePrompt } from '@/domain/contributor/encodeExercisePrompt';
import type {
  ContributorLessonApi,
  CreatedLesson,
  DraftExercise,
  DraftPhrase,
  LessonDraft,
  UploadAudioInput,
  UploadedAudio,
} from '@/domain/contributor/types';
import type { LessonDetail } from '@/domain/catalog/types';
import { ExerciseType, LessonStatus, SpeakerGender } from '@/domain/enums';
import { parseExercisePrompt } from '@/domain/exercises/parseExercisePrompt';
import { mapGetLessonResponse } from '@/lib/catalog/mapGetLessonResponse';
import { requireNumber, requireString, unwrapId, type IdWire } from '@/lib/catalog/wire';
import { apiRequest } from '@/utils/api';

function mapStatus(value: unknown): LessonStatus {
  if (typeof value === 'string' && (Object.values(LessonStatus) as string[]).includes(value)) {
    return value as LessonStatus;
  }

  return LessonStatus.Draft;
}

function mapCreated(body: unknown): CreatedLesson {
  const row = (body ?? {}) as { lessonId?: IdWire; status?: unknown };
  return {
    lessonId: unwrapId(row.lessonId, 'lessonId'),
    status: mapStatus(row.status),
  };
}

function mapExerciseFromDetail(exercise: LessonDetail['exercises'][number]): DraftExercise {
  const parsed = parseExercisePrompt({
    id: exercise.id,
    languageId: 'lang',
    lessonId: 'lesson',
    exerciseType: exercise.exerciseType,
    promptData: exercise.promptData,
    correctAnswer: exercise.correctAnswer,
    sortOrder: exercise.sortOrder,
  });

  const base: DraftExercise = {
    clientKey: exercise.id,
    id: exercise.id,
    exerciseType: (Object.values(ExerciseType) as string[]).includes(exercise.exerciseType)
      ? (exercise.exerciseType as ExerciseType)
      : ExerciseType.Flashcard,
    prompt: '',
    correctAnswer: exercise.correctAnswer,
    options: ['', '', '', ''],
    correctIndex: 0,
    tokens: '',
    recordingId: null,
    phraseId: null,
    sortOrder: exercise.sortOrder,
  };

  if (!parsed) {
    return base;
  }

  if (parsed.type === ExerciseType.Flashcard) {
    return { ...base, prompt: parsed.kalangaText, correctAnswer: parsed.correctAnswer, phraseId: parsed.phraseId };
  }

  if (parsed.type === ExerciseType.SentenceBuilder) {
    return {
      ...base,
      prompt: parsed.prompt,
      tokens: parsed.tokens.join(' '),
      correctAnswer: parsed.correctAnswer,
      phraseId: parsed.phraseId,
    };
  }

  return {
    ...base,
    prompt: parsed.prompt,
    options: [...parsed.options],
    correctIndex: parsed.correctIndex,
    recordingId: parsed.recordingId,
    correctAnswer: parsed.correctAnswer,
    phraseId: parsed.phraseId,
  };
}

export function lessonDetailToDraft(
  detail: LessonDetail,
  status: LessonStatus | null = LessonStatus.Draft,
): LessonDraft {
  return {
    id: detail.id,
    title: detail.title,
    level: detail.level,
    category: detail.category,
    isScenario: detail.isScenario,
    scenarioContext: detail.scenarioContext ?? '',
    xpReward: detail.xpReward,
    status,
    phrases: detail.phrases.map((phrase) => ({
      clientKey: phrase.id,
      id: phrase.id,
      kalangaText: phrase.kalangaText,
      englishTranslation: phrase.englishTranslation,
      sortOrder: phrase.sortOrder,
      variations: phrase.variations.map((variation) => ({
        clientKey: variation.id,
        id: variation.id,
        kalangaText: variation.kalangaText,
        registerLabel: variation.registerLabel,
      })),
      audio: phrase.audio,
    })),
    exercises: detail.exercises.map(mapExerciseFromDetail),
  };
}

function mapUploaded(body: unknown): UploadedAudio {
  const row = (body ?? {}) as {
    audioRecordingId?: IdWire;
    phraseId?: IdWire;
    cdnUrl?: unknown;
    durationMs?: unknown;
  };
  return {
    audioRecordingId: unwrapId(row.audioRecordingId, 'audioRecordingId'),
    phraseId: row.phraseId ? unwrapId(row.phraseId, 'phraseId') : null,
    cdnUrl: requireString(row.cdnUrl, 'cdnUrl'),
    durationMs: requireNumber(row.durationMs, 'durationMs'),
  };
}

function mapPhrasePayload(phrase: DraftPhrase, index: number) {
  return {
    phraseId: phrase.id,
    kalangaText: phrase.kalangaText.trim(),
    englishTranslation: phrase.englishTranslation.trim(),
    sortOrder: index,
    variations: phrase.variations.map((variation) => ({
      variationId: variation.id,
      kalangaText: variation.kalangaText.trim(),
      registerLabel: variation.registerLabel.trim(),
    })),
  };
}

function mapExercisePayload(exercise: DraftExercise, index: number) {
  const encoded = encodeExercisePrompt(exercise);
  return {
    exerciseId: exercise.id,
    exerciseType: exercise.exerciseType,
    promptData: encoded.promptData,
    correctAnswer: encoded.correctAnswer,
    sortOrder: index,
  };
}

export function createHttpContributorApi(): ContributorLessonApi {
  return {
    async createLesson(input) {
      try {
        const body = await apiRequest<unknown>('/lessons', {
          method: 'POST',
          body: {
            title: input.title,
            level: input.level,
            category: input.category,
            isScenario: input.isScenario,
            scenarioContext: input.scenarioContext,
            xpReward: input.xpReward,
          },
        });
        return mapCreated(body);
      } catch (error) {
        if (error instanceof AuthApiError) {
          throw new ContributorApiError(error.message);
        }

        throw error;
      }
    },
    async saveDraft(input) {
      try {
        const body = await apiRequest<unknown>(`/lessons/${input.lessonId}`, {
          method: 'PUT',
          body: {
            title: input.title,
            level: input.level,
            category: input.category,
            isScenario: input.isScenario,
            scenarioContext: input.scenarioContext,
            xpReward: input.xpReward,
            phrases: input.phrases.map((phrase, index) => mapPhrasePayload(phrase, index)),
            exercises: input.exercises.map((exercise, index) => mapExercisePayload(exercise, index)),
          },
        });
        return { ...mapCreated(body), lessonId: input.lessonId };
      } catch (error) {
        if (error instanceof AuthApiError) {
          throw new ContributorApiError(error.message);
        }

        throw error;
      }
    },
    async submitForReview(_languageId, lessonId) {
      try {
        const body = await apiRequest<{ status?: unknown }>(`/lessons/${lessonId}/submit`, { method: 'POST' });
        return mapStatus(body.status);
      } catch (error) {
        if (error instanceof AuthApiError) {
          throw new ContributorApiError(error.message);
        }

        throw error;
      }
    },
    async getDraft(_languageId, lessonId) {
      try {
        const body = await apiRequest<{ status?: unknown }>(`/lessons/${lessonId}/draft`, { method: 'GET' });
        return lessonDetailToDraft(mapGetLessonResponse(body), mapStatus(body.status));
      } catch (error) {
        if (error instanceof AuthApiError) {
          throw new ContributorApiError(error.message);
        }

        throw error;
      }
    },
    async uploadAudio(input: UploadAudioInput) {
      const form = new FormData();
      form.append('file', {
        uri: input.file.uri,
        name: input.file.name,
        type: input.file.mimeType,
      } as unknown as Blob);
      form.append('durationMs', String(input.durationMs));
      form.append('phraseId', input.phraseId);
      if (input.variationId) {
        form.append('variationId', input.variationId);
      }

      form.append('speakerGender', input.speakerGender ?? SpeakerGender.Unspecified);
      if (input.dialectLabel) {
        form.append('dialectLabel', input.dialectLabel);
      }

      try {
        const body = await apiRequest<unknown>('/audio/upload', {
          method: 'POST',
          formData: form,
          timeoutMs: AUDIO_UPLOAD_TIMEOUT_MS,
        });
        return mapUploaded(body);
      } catch (error) {
        if (error instanceof AuthApiError) {
          throw new ContributorApiError(error.message);
        }

        throw error;
      }
    },
  };
}
