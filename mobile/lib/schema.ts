/** ISO timestamps and calendar dates are stored as TEXT. Booleans are INTEGER 0/1. */

export const SCHEMA_VERSION = 1;

export const SCHEMA_SQL = `
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS schema_migrations (
  version INTEGER PRIMARY KEY NOT NULL,
  applied_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS lessons (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  title TEXT NOT NULL,
  level TEXT NOT NULL,
  category TEXT NOT NULL,
  is_scenario INTEGER NOT NULL DEFAULT 0,
  scenario_context TEXT,
  status TEXT NOT NULL,
  contributor_id TEXT NOT NULL,
  reviewed_by TEXT,
  review_feedback TEXT,
  xp_reward INTEGER NOT NULL DEFAULT 10,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_lessons_language_level
  ON lessons(language_id, level, status);

CREATE TABLE IF NOT EXISTS phrases (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  lesson_id TEXT NOT NULL,
  kalanga_text TEXT NOT NULL,
  english_translation TEXT NOT NULL,
  sort_order INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_phrases_lesson ON phrases(lesson_id);
CREATE INDEX IF NOT EXISTS idx_phrases_language ON phrases(language_id);

CREATE TABLE IF NOT EXISTS language_variations (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  phrase_id TEXT NOT NULL,
  kalanga_text TEXT NOT NULL,
  register_label TEXT NOT NULL,
  created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_language_variations_phrase ON language_variations(phrase_id);
CREATE INDEX IF NOT EXISTS idx_language_variations_language ON language_variations(language_id);

CREATE TABLE IF NOT EXISTS audio_recordings (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  phrase_id TEXT,
  variation_id TEXT,
  contributor_id TEXT NOT NULL,
  cdn_url TEXT NOT NULL,
  file_format TEXT NOT NULL,
  file_size_bytes INTEGER NOT NULL,
  speaker_gender TEXT NOT NULL,
  dialect_label TEXT,
  status TEXT NOT NULL,
  duration_ms INTEGER NOT NULL,
  created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_audio_phrase ON audio_recordings(phrase_id, status);
CREATE INDEX IF NOT EXISTS idx_audio_language ON audio_recordings(language_id);

CREATE TABLE IF NOT EXISTS exercises (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  lesson_id TEXT NOT NULL,
  exercise_type TEXT NOT NULL,
  prompt_data TEXT NOT NULL,
  correct_answer TEXT NOT NULL,
  sort_order INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_exercises_lesson ON exercises(lesson_id);
CREATE INDEX IF NOT EXISTS idx_exercises_language ON exercises(language_id);

CREATE TABLE IF NOT EXISTS learner_progress (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  lesson_id TEXT NOT NULL,
  completed_at TEXT,
  score INTEGER,
  xp_awarded INTEGER NOT NULL DEFAULT 0,
  updated_at TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_learner_progress_user_lesson
  ON learner_progress(user_id, lesson_id);
CREATE INDEX IF NOT EXISTS idx_progress_user
  ON learner_progress(user_id, language_id);

CREATE TABLE IF NOT EXISTS exercise_results (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  exercise_id TEXT NOT NULL,
  lesson_id TEXT NOT NULL,
  is_correct INTEGER NOT NULL,
  score INTEGER NOT NULL,
  answered_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_exercise_results_user
  ON exercise_results(user_id, language_id);

CREATE TABLE IF NOT EXISTS spaced_repetition_records (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  phrase_id TEXT NOT NULL,
  variation_id TEXT,
  ease_factor REAL NOT NULL DEFAULT 2.5,
  interval_days INTEGER NOT NULL DEFAULT 1,
  repetitions INTEGER NOT NULL DEFAULT 0,
  next_review_at TEXT NOT NULL,
  last_reviewed_at TEXT,
  updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_srs_user_next
  ON spaced_repetition_records(user_id, next_review_at);
CREATE UNIQUE INDEX IF NOT EXISTS ux_srs_user_phrase_base
  ON spaced_repetition_records(user_id, phrase_id)
  WHERE variation_id IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_srs_user_phrase_variation
  ON spaced_repetition_records(user_id, phrase_id, variation_id)
  WHERE variation_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS learner_gamification (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  total_xp INTEGER NOT NULL DEFAULT 0,
  current_streak INTEGER NOT NULL DEFAULT 0,
  longest_streak INTEGER NOT NULL DEFAULT 0,
  last_activity_date TEXT,
  progress_level TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_gamification_user_language
  ON learner_gamification(user_id, language_id);

CREATE TABLE IF NOT EXISTS requests (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  submitter_id TEXT NOT NULL,
  title TEXT NOT NULL,
  description TEXT NOT NULL,
  upvote_count INTEGER NOT NULL DEFAULT 0,
  status TEXT NOT NULL,
  fulfilled_by_lesson_id TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_requests_language_upvotes
  ON requests(language_id, upvote_count DESC);

CREATE TABLE IF NOT EXISTS sync_queue (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  client_operation_id TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  payload TEXT NOT NULL,
  attempts INTEGER NOT NULL DEFAULT 0,
  max_attempts INTEGER NOT NULL DEFAULT 5,
  next_attempt_at TEXT,
  status TEXT NOT NULL,
  last_error TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_sync_queue_client_operation
  ON sync_queue(language_id, user_id, client_operation_id);
CREATE INDEX IF NOT EXISTS idx_sync_queue_pending
  ON sync_queue(status, next_attempt_at);

CREATE TABLE IF NOT EXISTS content_packs (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  name TEXT NOT NULL,
  level TEXT,
  category TEXT,
  version INTEGER NOT NULL DEFAULT 1,
  size_bytes INTEGER NOT NULL,
  manifest_url TEXT NOT NULL,
  lesson_ids TEXT NOT NULL DEFAULT '[]',
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_content_packs_language ON content_packs(language_id);

CREATE TABLE IF NOT EXISTS download_progress (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  content_pack_id TEXT NOT NULL,
  recording_id TEXT NOT NULL,
  bytes_downloaded INTEGER NOT NULL DEFAULT 0,
  bytes_total INTEGER NOT NULL DEFAULT 0,
  local_path TEXT,
  status TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_download_progress_file
  ON download_progress(content_pack_id, recording_id);
CREATE INDEX IF NOT EXISTS idx_download_progress_language
  ON download_progress(language_id);

CREATE TABLE IF NOT EXISTS sync_checkpoints (
  id TEXT PRIMARY KEY NOT NULL,
  language_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  sync_version INTEGER NOT NULL DEFAULT 0,
  last_synced_at TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_sync_checkpoints_user_language
  ON sync_checkpoints(user_id, language_id);
`;

export const REQUIRED_TABLES = [
  'lessons',
  'phrases',
  'language_variations',
  'audio_recordings',
  'exercises',
  'learner_progress',
  'exercise_results',
  'spaced_repetition_records',
  'learner_gamification',
  'requests',
  'sync_queue',
  'content_packs',
  'download_progress',
  'sync_checkpoints',
] as const;
