import { finishPronunciationPractice, referenceSampleFromAudio } from '@/domain/pronunciation/finishPronunciationPractice';
import { PronunciationLabel } from '@/domain/pronunciation/types';

describe('finishPronunciationPractice', () => {
  it('scores on-device and does not upload without consent', async () => {
    const upload = jest.fn(async () => undefined);
    const result = await finishPronunciationPractice({
      learner: { uri: 'file:///docs/offline/pronunciation/a.m4a', durationMs: 1000, energyProfile: [] },
      reference: { durationMs: 1000, energyProfile: [] },
      consentGranted: false,
      transmitter: { upload },
    });

    expect(result.score.score).toBe(100);
    expect(result.score.label).toBe(PronunciationLabel.Excellent);
    expect(result.transmission).toBe('skipped');
    expect(upload).not.toHaveBeenCalled();
  });

  it('builds a duration-only reference sample from catalog audio', () => {
    expect(
      referenceSampleFromAudio({
        id: 'rec-1',
        cdnUrl: 'https://cdn.example/a.mp3',
        fileFormat: 'Mp3',
        speakerGender: 'Female',
        dialectLabel: null,
        durationMs: 1500,
      }),
    ).toEqual({ durationMs: 1500, energyProfile: [] });
  });
});
