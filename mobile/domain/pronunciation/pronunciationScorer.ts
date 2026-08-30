import {
  PRONUNCIATION_DURATION_WEIGHT,
  PRONUNCIATION_ENERGY_WEIGHT,
  PRONUNCIATION_SCORE_MAX,
  PRONUNCIATION_SCORE_MIN,
} from '@/constants/pronunciation';
import { labelForScore, type AudioSample, type PronunciationScore } from '@/domain/pronunciation/types';

function clampScore(value: number): number {
  if (Number.isNaN(value) || !Number.isFinite(value)) {
    return PRONUNCIATION_SCORE_MIN;
  }

  return Math.min(PRONUNCIATION_SCORE_MAX, Math.max(PRONUNCIATION_SCORE_MIN, Math.round(value)));
}

function durationSimilarity(learnerMs: number, referenceMs: number): number {
  const learner = Math.max(0, learnerMs);
  const reference = Math.max(0, referenceMs);
  if (learner === 0 && reference === 0) {
    return PRONUNCIATION_SCORE_MAX;
  }

  const longest = Math.max(learner, reference);
  if (longest === 0) {
    return PRONUNCIATION_SCORE_MIN;
  }

  return (Math.min(learner, reference) / longest) * PRONUNCIATION_SCORE_MAX;
}

function cosineSimilarity(left: readonly number[], right: readonly number[]): number | null {
  const length = Math.min(left.length, right.length);
  if (length === 0) {
    return null;
  }

  let dot = 0;
  let leftNorm = 0;
  let rightNorm = 0;
  for (let i = 0; i < length; i += 1) {
    const a = left[i] ?? 0;
    const b = right[i] ?? 0;
    dot += a * b;
    leftNorm += a * a;
    rightNorm += b * b;
  }

  if (leftNorm === 0 || rightNorm === 0) {
    return 0;
  }

  return (dot / Math.sqrt(leftNorm * rightNorm)) * PRONUNCIATION_SCORE_MAX;
}

export function scorePronunciation(learner: AudioSample, reference: AudioSample): PronunciationScore {
  const duration = durationSimilarity(learner.durationMs, reference.durationMs);
  const energy = cosineSimilarity(learner.energyProfile, reference.energyProfile);

  const score =
    energy === null
      ? duration
      : duration * PRONUNCIATION_DURATION_WEIGHT + energy * PRONUNCIATION_ENERGY_WEIGHT;

  const rounded = clampScore(score);
  return { score: rounded, label: labelForScore(rounded) };
}
