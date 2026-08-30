import { scorePronunciation } from '@/domain/pronunciation/pronunciationScorer';
import { PronunciationLabel } from '@/domain/pronunciation/types';

describe('PronunciationScorer', () => {
  it('returns a score in [0, 100] and a deterministic label', () => {
    const excellent = scorePronunciation(
      { durationMs: 1000, energyProfile: [0.5, 0.6, 0.4] },
      { durationMs: 1000, energyProfile: [0.5, 0.6, 0.4] },
    );
    expect(excellent.score).toBeGreaterThanOrEqual(0);
    expect(excellent.score).toBeLessThanOrEqual(100);
    expect(excellent.label).toBe(PronunciationLabel.Excellent);

    const weak = scorePronunciation(
      { durationMs: 200, energyProfile: [1, 0, 0] },
      { durationMs: 2000, energyProfile: [0, 0, 1] },
    );
    expect(weak.score).toBeGreaterThanOrEqual(0);
    expect(weak.score).toBeLessThanOrEqual(100);
    expect(weak.label).toBe(PronunciationLabel.TryAgain);

    expect(
      scorePronunciation(
        { durationMs: 800, energyProfile: [0.2, 0.3] },
        { durationMs: 1000, energyProfile: [0.2, 0.3] },
      ),
    ).toEqual(
      scorePronunciation(
        { durationMs: 800, energyProfile: [0.2, 0.3] },
        { durationMs: 1000, energyProfile: [0.2, 0.3] },
      ),
    );
  });

  it('uses duration only when energy profiles are missing', () => {
    const result = scorePronunciation(
      { durationMs: 500, energyProfile: [] },
      { durationMs: 1000, energyProfile: [] },
    );
    expect(result.score).toBe(50);
    expect(result.label).toBe(PronunciationLabel.TryAgain);
  });
});
