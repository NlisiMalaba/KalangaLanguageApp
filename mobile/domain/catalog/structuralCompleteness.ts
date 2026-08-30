import type { LessonDetail } from '@/domain/catalog/types';
import { Level } from '@/domain/enums';

const LEVELS = new Set<string>(Object.values(Level));

function hasText(value: string | null | undefined): boolean {
  return typeof value === 'string' && value.trim().length > 0;
}

/**
 * Property 7: a published lesson graph is complete only when title, level, category,
 * phrases, exercises, phrase audio, and variation labels are all present.
 */
export function isPublishedLessonStructurallyComplete(lesson: LessonDetail): boolean {
  if (!hasText(lesson.title) || !hasText(lesson.category) || !LEVELS.has(lesson.level)) {
    return false;
  }

  if (lesson.phrases.length === 0 || lesson.exercises.length === 0) {
    return false;
  }

  for (const phrase of lesson.phrases) {
    if (!hasText(phrase.kalangaText) || !hasText(phrase.englishTranslation)) {
      return false;
    }

    const hasPhraseAudio = phrase.audio.some((recording) => hasText(recording.id) && hasText(recording.cdnUrl));
    if (!hasPhraseAudio) {
      return false;
    }

    for (const variation of phrase.variations) {
      if (!hasText(variation.registerLabel)) {
        return false;
      }
    }
  }

  return true;
}
