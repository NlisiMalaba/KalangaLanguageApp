import { Level, LessonStatus } from '@/domain/enums';
import { boolFromSql, boolToSql, mapContentPack, mapLesson, type ContentPackRow, type LessonRow } from '@/lib/mappers';

describe('sqlite row mappers', () => {
  it('round-trips boolean flags as 0/1', () => {
    expect(boolToSql(true)).toBe(1);
    expect(boolToSql(false)).toBe(0);
    expect(boolFromSql(1)).toBe(true);
    expect(boolFromSql(0)).toBe(false);
  });

  it('maps a lesson row onto the domain Lesson', () => {
    const row: LessonRow = {
      id: 'lesson-1',
      language_id: 'lang-1',
      title: 'Greetings',
      level: Level.Beginner,
      category: 'everyday',
      is_scenario: 1,
      scenario_context: 'Market',
      status: LessonStatus.Published,
      contributor_id: 'user-1',
      reviewed_by: 'user-2',
      review_feedback: null,
      xp_reward: 10,
      created_at: '2026-08-23T12:00:00.000Z',
      updated_at: '2026-08-23T12:00:00.000Z',
    };

    expect(mapLesson(row)).toEqual({
      id: 'lesson-1',
      languageId: 'lang-1',
      title: 'Greetings',
      level: Level.Beginner,
      category: 'everyday',
      isScenario: true,
      scenarioContext: 'Market',
      status: LessonStatus.Published,
      contributorId: 'user-1',
      reviewedBy: 'user-2',
      reviewFeedback: null,
      xpReward: 10,
      createdAt: '2026-08-23T12:00:00.000Z',
      updatedAt: '2026-08-23T12:00:00.000Z',
    });
  });

  it('parses content pack lesson ids from JSON', () => {
    const row: ContentPackRow = {
      id: 'pack-1',
      language_id: 'lang-1',
      name: 'Beginner pack',
      level: Level.Beginner,
      category: null,
      version: 1,
      size_bytes: 1024,
      manifest_url: 'https://cdn.example.test/manifest.json',
      lesson_ids: '["lesson-1","lesson-2"]',
      created_at: '2026-08-23T12:00:00.000Z',
      updated_at: '2026-08-23T12:00:00.000Z',
    };

    expect(mapContentPack(row).lessonIds).toEqual(['lesson-1', 'lesson-2']);
  });
});
