import { DEFAULT_SRS_INTERVAL_DAYS, SRS_SECOND_INTERVAL_DAYS } from '@/constants/srs';
import { createSpacedRepetitionRecord } from '@/domain/srs/createSpacedRepetitionRecord';
import { recordAnswer } from '@/domain/srs/recordAnswer';

const now = new Date('2026-08-22T12:00:00.000Z');

describe('recordAnswer SM-2', () => {
  it('uses a 1-day then 6-day interval on the first two correct answers', () => {
    const created = createSpacedRepetitionRecord({
      languageId: 'lang-1',
      userId: 'user-1',
      phraseId: 'phrase-1',
      now,
    });
    expect(created.nextReviewAt).toBe('2026-08-22');

    const first = recordAnswer(created, true, now);
    expect(first.repetitions).toBe(1);
    expect(first.intervalDays).toBe(DEFAULT_SRS_INTERVAL_DAYS);
    expect(first.nextReviewAt).toBe('2026-08-23');
    expect(first.easeFactor).toBe(2.6);

    const second = recordAnswer(first, true, now);
    expect(second.repetitions).toBe(2);
    expect(second.intervalDays).toBe(SRS_SECOND_INTERVAL_DAYS);
    expect(second.nextReviewAt).toBe('2026-08-28');
  });

  it('lengthens a mature interval on a correct answer and resets it on an incorrect one', () => {
    const mature = {
      ...createSpacedRepetitionRecord({
        languageId: 'lang-1',
        userId: 'user-1',
        phraseId: 'phrase-1',
        now,
      }),
      easeFactor: 2.5,
      intervalDays: 10,
      repetitions: 3,
      nextReviewAt: '2026-09-01',
    };

    const correct = recordAnswer(mature, true, now);
    expect(correct.intervalDays).toBeGreaterThan(10);
    expect(correct.repetitions).toBe(4);
    expect(correct.nextReviewAt).toBe('2026-09-17');
    expect(correct.easeFactor).toBeGreaterThanOrEqual(2.5);

    const incorrect = recordAnswer(mature, false, now);
    expect(incorrect.repetitions).toBe(0);
    expect(incorrect.intervalDays).toBe(DEFAULT_SRS_INTERVAL_DAYS);
    expect(incorrect.nextReviewAt).toBe('2026-08-23');
    expect(incorrect.easeFactor).toBeLessThanOrEqual(mature.easeFactor);
  });
});
