import type { AudioRecording, EntityId } from '@/domain/entities';
import { AudioRecordingStatus } from '@/domain/enums';
import { withStore } from '@/lib/database';
import type { LocalStore } from '@/lib/localStore';
import { requireLanguageId, requireTenantMatch } from '@/lib/localStore';
import { mapAudioRecording, type AudioRecordingRow } from '@/lib/mappers';
import { sqlInPlaceholders } from '@/lib/sqlIn';

const UPSERT_SQL = `
INSERT INTO audio_recordings (
  id, language_id, phrase_id, variation_id, contributor_id, cdn_url, file_format,
  file_size_bytes, speaker_gender, dialect_label, status, duration_ms, created_at
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
ON CONFLICT(id) DO UPDATE SET
  phrase_id = excluded.phrase_id,
  variation_id = excluded.variation_id,
  cdn_url = excluded.cdn_url,
  file_format = excluded.file_format,
  file_size_bytes = excluded.file_size_bytes,
  speaker_gender = excluded.speaker_gender,
  dialect_label = excluded.dialect_label,
  status = excluded.status,
  duration_ms = excluded.duration_ms
WHERE audio_recordings.language_id = excluded.language_id
`;

export async function upsertAudioRecording(
  languageId: EntityId,
  recording: AudioRecording,
  store?: LocalStore,
): Promise<void> {
  const tenant = requireTenantMatch(languageId, recording.languageId);
  await withStore(store, (db) =>
    db.run(UPSERT_SQL, [
      recording.id,
      tenant,
      recording.phraseId,
      recording.variationId,
      recording.contributorId,
      recording.cdnUrl,
      recording.fileFormat,
      recording.fileSizeBytes,
      recording.speakerGender,
      recording.dialectLabel,
      recording.status,
      recording.durationMs,
      recording.createdAt,
    ]),
  );
}

export async function listApprovedAudioForPhrase(
  languageId: EntityId,
  phraseId: EntityId,
  store?: LocalStore,
): Promise<AudioRecording[]> {
  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<AudioRecordingRow>(
      `SELECT * FROM audio_recordings
       WHERE language_id = ? AND phrase_id = ? AND variation_id IS NULL AND status = ?`,
      [tenant, phraseId, AudioRecordingStatus.Approved],
    );
    return rows.map(mapAudioRecording);
  });
}

export async function listApprovedAudioForPhrases(
  languageId: EntityId,
  phraseIds: readonly EntityId[],
  store?: LocalStore,
): Promise<AudioRecording[]> {
  if (phraseIds.length === 0) {
    return [];
  }

  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<AudioRecordingRow>(
      `SELECT * FROM audio_recordings
       WHERE language_id = ? AND variation_id IS NULL AND status = ?
         AND phrase_id IN (${sqlInPlaceholders(phraseIds.length)})`,
      [tenant, AudioRecordingStatus.Approved, ...phraseIds],
    );
    return rows.map(mapAudioRecording);
  });
}

export async function listApprovedAudioForVariations(
  languageId: EntityId,
  variationIds: readonly EntityId[],
  store?: LocalStore,
): Promise<AudioRecording[]> {
  if (variationIds.length === 0) {
    return [];
  }

  const tenant = requireLanguageId(languageId);
  return withStore(store, async (db) => {
    const rows = await db.getAll<AudioRecordingRow>(
      `SELECT * FROM audio_recordings
       WHERE language_id = ? AND status = ?
         AND variation_id IN (${sqlInPlaceholders(variationIds.length)})`,
      [tenant, AudioRecordingStatus.Approved, ...variationIds],
    );
    return rows.map(mapAudioRecording);
  });
}
