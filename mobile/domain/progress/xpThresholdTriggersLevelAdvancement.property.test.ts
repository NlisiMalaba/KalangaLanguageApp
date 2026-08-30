import * as fc from 'fast-check';

import { ADVANCED_XP_THRESHOLD, INTERMEDIATE_XP_THRESHOLD } from '@/constants/progress';
import { Level } from '@/domain/enums';
import {
  advanceLevelIfThresholdCrossed,
  awardXp,
  createLearnerGamification,
} from '@/domain/progress/gamification';

describe('XP threshold triggers level advancement', () => {
  // Feature: kalanga-language-app, Property 19: XP Threshold Triggers Level Advancement
  it('sets progress level from XP thresholds and is idempotent after the first advance', () => {
    fc.assert(
      fc.property(fc.integer({ min: 1, max: ADVANCED_XP_THRESHOLD + 500 }), (xp) => {
        const now = new Date('2026-08-22T12:00:00.000Z');
        const awarded = awardXp(
          createLearnerGamification({ languageId: 'lang-1', userId: 'user-1', now }),
          xp,
          now,
        );
        expect(awarded.progressLevel).toBe(Level.Beginner);
        expect(awarded.totalXp).toBe(xp);

        const expected =
          xp >= ADVANCED_XP_THRESHOLD
            ? Level.Advanced
            : xp >= INTERMEDIATE_XP_THRESHOLD
              ? Level.Intermediate
              : Level.Beginner;

        const first = advanceLevelIfThresholdCrossed(awarded, now);
        expect(first.progressLevel).toBe(expected);
        expect(first.totalXp).toBe(xp);

        const second = advanceLevelIfThresholdCrossed(first, now);
        expect(second.progressLevel).toBe(expected);
        expect(second).toBe(first);
      }),
      { numRuns: 100 },
    );
  });
});
