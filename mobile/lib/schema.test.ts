import { REQUIRED_TABLES, SCHEMA_SQL } from '@/lib/schema';

describe('local sqlite schema', () => {
  it('defines every required table', () => {
    for (const table of REQUIRED_TABLES) {
      expect(SCHEMA_SQL).toContain(`CREATE TABLE IF NOT EXISTS ${table} (`);
    }
  });

  it('scopes catalog and learner tables by language_id', () => {
    for (const table of REQUIRED_TABLES) {
      expect(SCHEMA_SQL).toMatch(
        new RegExp(`CREATE TABLE IF NOT EXISTS ${table} \\([\\s\\S]*?language_id TEXT NOT NULL`),
      );
    }
  });

  it('enforces unique progress, SRS, gamification, sync, and download keys', () => {
    expect(SCHEMA_SQL).toContain(
      'CREATE UNIQUE INDEX IF NOT EXISTS ux_learner_progress_user_lesson',
    );
    expect(SCHEMA_SQL).toContain('CREATE UNIQUE INDEX IF NOT EXISTS ux_srs_user_phrase_base');
    expect(SCHEMA_SQL).toContain('CREATE UNIQUE INDEX IF NOT EXISTS ux_srs_user_phrase_variation');
    expect(SCHEMA_SQL).toContain('CREATE UNIQUE INDEX IF NOT EXISTS ux_gamification_user_language');
    expect(SCHEMA_SQL).toContain('CREATE UNIQUE INDEX IF NOT EXISTS ux_sync_queue_client_operation');
    expect(SCHEMA_SQL).toContain('CREATE UNIQUE INDEX IF NOT EXISTS ux_download_progress_file');
  });
});
