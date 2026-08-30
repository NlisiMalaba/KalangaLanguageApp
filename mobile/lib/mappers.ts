import type {
  AudioRecording,
  ContentPack,
  Exercise,
  LanguageVariation,
  LearnerGamification,
  LearnerProgress,
  Lesson,
  Phrase,
  Request,
  SpacedRepetitionRecord,
} from '@/domain/entities';
import type {
  AudioFileFormat,
  AudioRecordingStatus,
  ExerciseType,
  Level,
  LessonStatus,
  RequestStatus,
  SpeakerGender,
} from '@/domain/enums';

export type LessonRow = {
  id: string;
  language_id: string;
  title: string;
  level: string;
  category: string;
  is_scenario: number;
  scenario_context: string | null;
  status: string;
  contributor_id: string;
  reviewed_by: string | null;
  review_feedback: string | null;
  xp_reward: number;
  created_at: string;
  updated_at: string;
};

export type PhraseRow = {
  id: string;
  language_id: string;
  lesson_id: string;
  kalanga_text: string;
  english_translation: string;
  sort_order: number;
  created_at: string;
};

export type LanguageVariationRow = {
  id: string;
  language_id: string;
  phrase_id: string;
  kalanga_text: string;
  register_label: string;
  created_at: string;
};

export type AudioRecordingRow = {
  id: string;
  language_id: string;
  phrase_id: string | null;
  variation_id: string | null;
  contributor_id: string;
  cdn_url: string;
  file_format: string;
  file_size_bytes: number;
  speaker_gender: string;
  dialect_label: string | null;
  status: string;
  duration_ms: number;
  created_at: string;
};

export type ExerciseRow = {
  id: string;
  language_id: string;
  lesson_id: string;
  exercise_type: string;
  prompt_data: string;
  correct_answer: string;
  sort_order: number;
  created_at: string;
};

export type LearnerProgressRow = {
  id: string;
  language_id: string;
  user_id: string;
  lesson_id: string;
  completed_at: string | null;
  score: number | null;
  xp_awarded: number;
  updated_at: string;
};

export type SpacedRepetitionRow = {
  id: string;
  language_id: string;
  user_id: string;
  phrase_id: string;
  variation_id: string | null;
  ease_factor: number;
  interval_days: number;
  repetitions: number;
  next_review_at: string;
  last_reviewed_at: string | null;
  updated_at: string;
};

export type LearnerGamificationRow = {
  id: string;
  language_id: string;
  user_id: string;
  total_xp: number;
  current_streak: number;
  longest_streak: number;
  last_activity_date: string | null;
  progress_level: string;
  updated_at: string;
};

export type RequestRow = {
  id: string;
  language_id: string;
  submitter_id: string;
  title: string;
  description: string;
  upvote_count: number;
  status: string;
  fulfilled_by_lesson_id: string | null;
  created_at: string;
  updated_at: string;
};

export type ContentPackRow = {
  id: string;
  language_id: string;
  name: string;
  level: string | null;
  category: string | null;
  version: number;
  size_bytes: number;
  manifest_url: string;
  lesson_ids: string;
  created_at: string;
  updated_at: string;
};

export function boolToSql(value: boolean): number {
  return value ? 1 : 0;
}

export function boolFromSql(value: number): boolean {
  return value === 1;
}

export function mapLesson(row: LessonRow): Lesson {
  return {
    id: row.id,
    languageId: row.language_id,
    title: row.title,
    level: row.level as Level,
    category: row.category,
    isScenario: boolFromSql(row.is_scenario),
    scenarioContext: row.scenario_context,
    status: row.status as LessonStatus,
    contributorId: row.contributor_id,
    reviewedBy: row.reviewed_by,
    reviewFeedback: row.review_feedback,
    xpReward: row.xp_reward,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

export function mapPhrase(row: PhraseRow): Phrase {
  return {
    id: row.id,
    languageId: row.language_id,
    lessonId: row.lesson_id,
    kalangaText: row.kalanga_text,
    englishTranslation: row.english_translation,
    sortOrder: row.sort_order,
    createdAt: row.created_at,
  };
}

export function mapLanguageVariation(row: LanguageVariationRow): LanguageVariation {
  return {
    id: row.id,
    languageId: row.language_id,
    phraseId: row.phrase_id,
    kalangaText: row.kalanga_text,
    registerLabel: row.register_label,
    createdAt: row.created_at,
  };
}

export function mapAudioRecording(row: AudioRecordingRow): AudioRecording {
  return {
    id: row.id,
    languageId: row.language_id,
    phraseId: row.phrase_id,
    variationId: row.variation_id,
    contributorId: row.contributor_id,
    cdnUrl: row.cdn_url,
    fileFormat: row.file_format as AudioFileFormat,
    fileSizeBytes: row.file_size_bytes,
    speakerGender: row.speaker_gender as SpeakerGender,
    dialectLabel: row.dialect_label,
    status: row.status as AudioRecordingStatus,
    durationMs: row.duration_ms,
    createdAt: row.created_at,
  };
}

export function mapExercise(row: ExerciseRow): Exercise {
  return {
    id: row.id,
    languageId: row.language_id,
    lessonId: row.lesson_id,
    exerciseType: row.exercise_type as ExerciseType,
    promptData: row.prompt_data,
    correctAnswer: row.correct_answer,
    sortOrder: row.sort_order,
    createdAt: row.created_at,
  };
}

export function mapLearnerProgress(row: LearnerProgressRow): LearnerProgress {
  return {
    id: row.id,
    languageId: row.language_id,
    userId: row.user_id,
    lessonId: row.lesson_id,
    completedAt: row.completed_at,
    score: row.score,
    xpAwarded: row.xp_awarded,
    updatedAt: row.updated_at,
  };
}

export function mapSpacedRepetition(row: SpacedRepetitionRow): SpacedRepetitionRecord {
  return {
    id: row.id,
    languageId: row.language_id,
    userId: row.user_id,
    phraseId: row.phrase_id,
    variationId: row.variation_id,
    easeFactor: row.ease_factor,
    intervalDays: row.interval_days,
    repetitions: row.repetitions,
    nextReviewAt: row.next_review_at,
    lastReviewedAt: row.last_reviewed_at,
    updatedAt: row.updated_at,
  };
}

export function mapLearnerGamification(row: LearnerGamificationRow): LearnerGamification {
  return {
    id: row.id,
    languageId: row.language_id,
    userId: row.user_id,
    totalXp: row.total_xp,
    currentStreak: row.current_streak,
    longestStreak: row.longest_streak,
    lastActivityDate: row.last_activity_date,
    progressLevel: row.progress_level as Level,
    updatedAt: row.updated_at,
  };
}

export function mapRequest(row: RequestRow): Request {
  return {
    id: row.id,
    languageId: row.language_id,
    submitterId: row.submitter_id,
    title: row.title,
    description: row.description,
    upvoteCount: row.upvote_count,
    status: row.status as RequestStatus,
    fulfilledByLessonId: row.fulfilled_by_lesson_id,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

export function mapContentPack(row: ContentPackRow): ContentPack {
  const parsed: unknown = JSON.parse(row.lesson_ids);
  const lessonIds = Array.isArray(parsed) ? parsed.map(String) : [];

  return {
    id: row.id,
    languageId: row.language_id,
    name: row.name,
    level: row.level as Level | null,
    category: row.category,
    version: row.version,
    sizeBytes: row.size_bytes,
    manifestUrl: row.manifest_url,
    lessonIds,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}
