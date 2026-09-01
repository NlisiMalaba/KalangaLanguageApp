import type { EntityId } from '@/domain/entities';
import type { ExerciseType, Level, LessonStatus, SpeakerGender } from '@/domain/enums';
import type { AudioRef } from '@/domain/catalog/types';

export type DraftVariation = {
  clientKey: string;
  id: EntityId | null;
  kalangaText: string;
  registerLabel: string;
};

export type DraftPhrase = {
  clientKey: string;
  id: EntityId | null;
  kalangaText: string;
  englishTranslation: string;
  sortOrder: number;
  variations: DraftVariation[];
  audio: AudioRef[];
};

export type DraftExercise = {
  clientKey: string;
  id: EntityId | null;
  exerciseType: ExerciseType;
  prompt: string;
  correctAnswer: string;
  options: string[];
  correctIndex: number;
  tokens: string;
  recordingId: EntityId | null;
  phraseId: EntityId | null;
  sortOrder: number;
};

export type LessonDraft = {
  id: EntityId | null;
  title: string;
  level: Level;
  category: string;
  isScenario: boolean;
  scenarioContext: string;
  xpReward: number;
  status: LessonStatus | null;
  phrases: DraftPhrase[];
  exercises: DraftExercise[];
};

export type CreateLessonInput = {
  languageId: EntityId;
  title: string;
  level: Level;
  category: string;
  isScenario: boolean;
  scenarioContext: string | null;
  xpReward: number;
};

export type SaveLessonDraftInput = CreateLessonInput & {
  lessonId: EntityId;
  phrases: DraftPhrase[];
  exercises: DraftExercise[];
};

export type CreatedLesson = {
  lessonId: EntityId;
  status: LessonStatus;
};

export type PickedAudioFile = {
  uri: string;
  name: string;
  mimeType: string;
  sizeBytes: number | null;
};

export type UploadAudioInput = {
  file: PickedAudioFile;
  durationMs: number;
  phraseId: EntityId;
  variationId?: EntityId | null;
  speakerGender?: SpeakerGender;
  dialectLabel?: string | null;
};

export type UploadedAudio = {
  audioRecordingId: EntityId;
  phraseId: EntityId | null;
  cdnUrl: string;
  durationMs: number;
};

export type ContributorLessonApi = {
  createLesson: (input: CreateLessonInput) => Promise<CreatedLesson>;
  saveDraft: (input: SaveLessonDraftInput) => Promise<CreatedLesson>;
  submitForReview: (languageId: EntityId, lessonId: EntityId) => Promise<LessonStatus>;
  getDraft: (languageId: EntityId, lessonId: EntityId) => Promise<LessonDraft>;
  uploadAudio: (input: UploadAudioInput) => Promise<UploadedAudio>;
};
