import type { AudioRef, LessonPhraseDetail } from '@/domain/catalog/types';

export function recordingsForPhrase(phrase: LessonPhraseDetail): AudioRef[] {
  const seen = new Set<string>();
  const recordings: AudioRef[] = [];

  for (const recording of [...phrase.audio, ...phrase.variations.flatMap((variation) => variation.audio)]) {
    if (seen.has(recording.id)) {
      continue;
    }

    seen.add(recording.id);
    recordings.push(recording);
  }

  return recordings;
}
