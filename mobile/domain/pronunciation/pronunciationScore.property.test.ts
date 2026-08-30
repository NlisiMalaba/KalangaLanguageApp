import * as fc from 'fast-check';

import {
  PRONUNCIATION_SCORE_MAX,
  PRONUNCIATION_SCORE_MIN,
} from '@/constants/pronunciation';
import { scorePronunciation } from '@/domain/pronunciation/pronunciationScorer';
import { PronunciationLabel, labelForScore, type AudioSample } from '@/domain/pronunciation/types';

const LABELS = [
  PronunciationLabel.Excellent,
  PronunciationLabel.Good,
  PronunciationLabel.TryAgain,
] as const;

const sampleArb: fc.Arbitrary<AudioSample> = fc.record({
  durationMs: fc.integer({ min: 0, max: 30_000 }),
  energyProfile: fc.array(fc.double({ min: 0, max: 1, noNaN: true }), { minLength: 0, maxLength: 16 }),
});

describe('pronunciation score range and feedback determinism', () => {
  // Feature: kalanga-language-app, Property 11: Pronunciation Score Range and Feedback Determinism
  it('scores every recording pair in [0, 100] with a label derived only from that score', () => {
    fc.assert(
      fc.property(sampleArb, sampleArb, (learner, reference) => {
        const first = scorePronunciation(learner, reference);
        const second = scorePronunciation(learner, reference);

        expect(first.score).toBeGreaterThanOrEqual(PRONUNCIATION_SCORE_MIN);
        expect(first.score).toBeLessThanOrEqual(PRONUNCIATION_SCORE_MAX);
        expect(Number.isInteger(first.score)).toBe(true);
        expect(first.label.length).toBeGreaterThan(0);
        expect(LABELS).toContain(first.label);
        expect(first.label).toBe(labelForScore(first.score));
        expect(second).toEqual(first);
      }),
      { numRuns: 100 },
    );
  });

  it('maps any score in range to the same non-empty label every time', () => {
    fc.assert(
      fc.property(fc.integer({ min: PRONUNCIATION_SCORE_MIN, max: PRONUNCIATION_SCORE_MAX }), (score) => {
        const first = labelForScore(score);
        const second = labelForScore(score);
        expect(first.length).toBeGreaterThan(0);
        expect(LABELS).toContain(first);
        expect(second).toBe(first);
      }),
      { numRuns: 100 },
    );
  });
});
