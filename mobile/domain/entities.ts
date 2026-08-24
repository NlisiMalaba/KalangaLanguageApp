import type {
  AudioFileFormat,
  AudioRecordingStatus,
  ExerciseType,
  Level,
  LessonStatus,
  RequestStatus,
  Role,
  SpeakerGender,
  UserStatus,
} from './enums';

/** UUID string (backend Guid / *Id value objects). */
export type EntityId = string;

/** ISO-8601 instant with offset, e.g. 2026-08-23T12:00:00.000Z */
export type Instant = string;

/** Calendar date YYYY-MM-DD (backend DateOnly). */
export type CalendarDate = string;

export interface Language {
  id: EntityId;
  code: string;
  name: string;
  region: string;
  isActive: boolean;
  createdAt: Instant;
}

/** Profile fields only — password hashes stay in expo-secure-store, never on this type. */
export interface User {
  id: EntityId;
  languageId: EntityId;
  email: string;
  displayName: string;
  role: Role;
  status: UserStatus;
  createdAt: Instant;
  updatedAt: Instant;
}

export interface Lesson {
  id: EntityId;
  languageId: EntityId;
  title: string;
  level: Level;
  category: string;
  isScenario: boolean;
  scenarioContext: string | null;
  status: LessonStatus;
  contributorId: EntityId;
  reviewedBy: EntityId | null;
  reviewFeedback: string | null;
  xpReward: number;
  createdAt: Instant;
  updatedAt: Instant;
}

export interface Phrase {
  id: EntityId;
  languageId: EntityId;
  lessonId: EntityId;
  kalangaText: string;
  englishTranslation: string;
  sortOrder: number;
  createdAt: Instant;
}

export interface LanguageVariation {
  id: EntityId;
  languageId: EntityId;
  phraseId: EntityId;
  kalangaText: string;
  registerLabel: string;
  createdAt: Instant;
}

export interface AudioRecording {
  id: EntityId;
  languageId: EntityId;
  phraseId: EntityId | null;
  variationId: EntityId | null;
  contributorId: EntityId;
  cdnUrl: string;
  fileFormat: AudioFileFormat;
  fileSizeBytes: number;
  speakerGender: SpeakerGender;
  dialectLabel: string | null;
  status: AudioRecordingStatus;
  durationMs: number;
  createdAt: Instant;
}

export interface Exercise {
  id: EntityId;
  languageId: EntityId;
  lessonId: EntityId;
  exerciseType: ExerciseType;
  promptData: string;
  correctAnswer: string;
  sortOrder: number;
  createdAt: Instant;
}

export interface LearnerProgress {
  id: EntityId;
  languageId: EntityId;
  userId: EntityId;
  lessonId: EntityId;
  completedAt: Instant | null;
  score: number | null;
  xpAwarded: number;
  updatedAt: Instant;
}

export interface SpacedRepetitionRecord {
  id: EntityId;
  languageId: EntityId;
  userId: EntityId;
  phraseId: EntityId;
  variationId: EntityId | null;
  easeFactor: number;
  intervalDays: number;
  repetitions: number;
  nextReviewAt: CalendarDate;
  lastReviewedAt: Instant | null;
  updatedAt: Instant;
}

export interface LearnerGamification {
  id: EntityId;
  languageId: EntityId;
  userId: EntityId;
  totalXp: number;
  currentStreak: number;
  longestStreak: number;
  lastActivityDate: CalendarDate | null;
  progressLevel: Level;
  updatedAt: Instant;
}

export interface Request {
  id: EntityId;
  languageId: EntityId;
  submitterId: EntityId;
  title: string;
  description: string;
  upvoteCount: number;
  status: RequestStatus;
  fulfilledByLessonId: EntityId | null;
  createdAt: Instant;
  updatedAt: Instant;
}

export interface ContentPack {
  id: EntityId;
  languageId: EntityId;
  name: string;
  level: Level | null;
  category: string | null;
  version: number;
  sizeBytes: number;
  manifestUrl: string;
  lessonIds: readonly EntityId[];
  createdAt: Instant;
  updatedAt: Instant;
}
