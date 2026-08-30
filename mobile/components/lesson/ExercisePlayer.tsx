import { Flashcard } from '@/components/lesson/Flashcard';
import { ListeningMultipleChoiceMode } from '@/components/lesson/ListeningMultipleChoiceMode';
import { MultipleChoiceMode } from '@/components/lesson/MultipleChoiceMode';
import { SentenceBuilder } from '@/components/lesson/SentenceBuilder';
import { recordingsForPhrase } from '@/domain/audio/phraseRecordings';
import type { LessonDetail } from '@/domain/catalog/types';
import { ExerciseType } from '@/domain/enums';
import type { ExerciseAttempt, ParsedExercise } from '@/domain/exercises/types';

export function ExercisePlayer({
  lesson,
  exercise,
  disabled,
  selectedIndex,
  onAttempt,
  onHeardMs,
}: {
  lesson: LessonDetail;
  exercise: ParsedExercise;
  disabled: boolean;
  selectedIndex: number | null;
  onAttempt: (attempt: ExerciseAttempt) => void;
  onHeardMs?: (durationMs: number) => void;
}) {
  if (exercise.type === ExerciseType.Flashcard) {
    return (
      <Flashcard
        kalangaText={exercise.kalangaText}
        disabled={disabled}
        onSubmitTranslation={(translation) =>
          onAttempt({ kind: ExerciseType.Flashcard, translation })
        }
        onSelfGrade={(remembered) => onAttempt({ kind: ExerciseType.Flashcard, remembered })}
      />
    );
  }

  if (exercise.type === ExerciseType.MultipleChoice) {
    return (
      <MultipleChoiceMode
        prompt={exercise.prompt || 'Choose the translation'}
        options={exercise.options}
        selectedIndex={selectedIndex}
        disabled={disabled}
        onSelect={(index) => onAttempt({ kind: ExerciseType.MultipleChoice, selectedIndex: index })}
      />
    );
  }

  if (exercise.type === ExerciseType.Listening) {
    const phrase = lesson.phrases.find((item) => item.id === exercise.phraseId);
    return (
      <ListeningMultipleChoiceMode
        languageId={lesson.languageId}
        recordings={phrase ? recordingsForPhrase(phrase) : []}
        ttsText={exercise.prompt || phrase?.kalangaText || ''}
        options={exercise.options}
        selectedIndex={selectedIndex}
        disabled={disabled}
        onSelect={(index) => onAttempt({ kind: ExerciseType.Listening, selectedIndex: index })}
        onHeardMs={onHeardMs}
      />
    );
  }

  if (exercise.type === ExerciseType.SentenceBuilder) {
    return (
      <SentenceBuilder
        prompt={exercise.prompt}
        tokens={exercise.tokens}
        disabled={disabled}
        onSubmit={(order) => onAttempt({ kind: ExerciseType.SentenceBuilder, order })}
      />
    );
  }

  return null;
}
