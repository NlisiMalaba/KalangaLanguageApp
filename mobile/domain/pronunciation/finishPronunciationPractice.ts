import { scorePronunciation } from '@/domain/pronunciation/pronunciationScorer';
import { transmitPronunciationIfConsented } from '@/domain/pronunciation/transmitPronunciation';
import type {
  AudioSample,
  LocalPronunciationRecording,
  PronunciationScore,
  PronunciationTransmitter,
} from '@/domain/pronunciation/types';
import type { AudioRef } from '@/domain/catalog/types';

export type FinishPronunciationPracticeInput = {
  learner: LocalPronunciationRecording;
  reference: AudioSample;
  consentGranted: boolean;
  transmitter: PronunciationTransmitter;
};

export type FinishPronunciationPracticeResult = {
  score: PronunciationScore;
  transmission: 'skipped' | 'uploaded';
};

export function referenceSampleFromAudio(recording: AudioRef | undefined): AudioSample {
  return {
    durationMs: Math.max(0, recording?.durationMs ?? 0),
    energyProfile: [],
  };
}

export async function finishPronunciationPractice(
  input: FinishPronunciationPracticeInput,
): Promise<FinishPronunciationPracticeResult> {
  const score = scorePronunciation(
    { durationMs: input.learner.durationMs, energyProfile: input.learner.energyProfile },
    input.reference,
  );
  const transmission = await transmitPronunciationIfConsented(
    input.learner.uri,
    input.consentGranted,
    input.transmitter,
  );
  return { score, transmission };
}
