import { useCallback, useEffect, useRef, useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';
import { toast } from 'sonner-native';

import { ExercisePlayer } from '@/components/lesson/ExercisePlayer';
import { LessonCompleteScreen } from '@/components/lesson/LessonCompleteScreen';
import { ThemedText } from '@/components/themed-text';
import type { LessonDetail } from '@/domain/catalog/types';
import { Level } from '@/domain/enums';
import { ExerciseError } from '@/domain/exercises/errors';
import { gradeExercise } from '@/domain/exercises/gradeExercise';
import type { PreparedExercise } from '@/domain/exercises/prepareExercises';
import type { ExerciseAttempt, GradeResult } from '@/domain/exercises/types';
import { prepareReviewExercises, type ReviewPrompt } from '@/domain/progress/dailyReview';
import type { EntityId } from '@/domain/entities';
import type { SpacedRepetitionEngine } from '@/domain/srs/spacedRepetitionEngine';
import { addListeningMs } from '@/lib/speakingListeningStats';

function stubLesson(languageId: EntityId): LessonDetail {
  return {
    id: 'daily-review',
    languageId,
    title: 'Daily review',
    level: Level.Beginner,
    category: 'Review',
    isScenario: false,
    scenarioContext: null,
    xpReward: 0,
    updatedAt: new Date().toISOString(),
    phrases: [],
    exercises: [],
  };
}

export function DailyReviewSequence({
  languageId,
  userId,
  loadPrompts,
  srs,
  onContinue,
}: {
  languageId: EntityId;
  userId: EntityId;
  loadPrompts: () => Promise<ReviewPrompt[]>;
  srs: SpacedRepetitionEngine;
  onContinue: () => void;
}) {
  const [queue, setQueue] = useState<PreparedExercise[]>([]);
  const [prompts, setPrompts] = useState<ReviewPrompt[]>([]);
  const [index, setIndex] = useState(0);
  const [result, setResult] = useState<GradeResult | null>(null);
  const [correctCount, setCorrectCount] = useState(0);
  const [complete, setComplete] = useState(false);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const correctCountRef = useRef(0);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const next = await loadPrompts();
      setPrompts(next);
      setQueue(prepareReviewExercises(next));
      setIndex(0);
      setResult(null);
      setCorrectCount(0);
      correctCountRef.current = 0;
      setComplete(false);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Could not load daily review.';
      toast.error(message);
    } finally {
      setLoading(false);
    }
  }, [loadPrompts]);

  useEffect(() => {
    void load();
  }, [load]);

  const current = queue[index] ?? null;
  const prompt = prompts[index] ?? null;

  const onAttempt = (attempt: ExerciseAttempt) => {
    if (!current || !prompt || result || busy) {
      return;
    }

    setBusy(true);
    try {
      const grade = gradeExercise(current.parsed, attempt);
      setResult(grade);
      if (grade.correct) {
        correctCountRef.current += 1;
        setCorrectCount(correctCountRef.current);
      }

      void srs
        .recordAnswer({
          languageId,
          userId,
          phraseId: prompt.card.phraseId,
          variationId: prompt.card.variationId,
          isCorrect: grade.correct,
        })
        .catch((error) => {
          const message = error instanceof Error ? error.message : 'Could not update review schedule.';
          toast.error(message);
        });
    } catch (error) {
      const message = error instanceof ExerciseError ? error.message : 'Could not score that answer.';
      toast.error(message);
    } finally {
      setBusy(false);
    }
  };

  if (loading) {
    return <ThemedText>Loading daily review…</ThemedText>;
  }

  if (queue.length === 0) {
    return <ThemedText>No phrases are due for review today.</ThemedText>;
  }

  if (complete) {
    return (
      <LessonCompleteScreen
        stats={{
          title: 'Daily review',
          xpReward: 0,
          correctCount,
          totalCount: queue.length,
        }}
        onContinue={onContinue}
        onPracticeAgain={() => void load()}
      />
    );
  }

  if (!current) {
    return null;
  }

  return (
    <View style={styles.container}>
      <ThemedText accessibilityLabel={`Review ${index + 1} of ${queue.length}`}>
        Review {index + 1} of {queue.length}
      </ThemedText>
      <ExercisePlayer
        key={current.source.id}
        lesson={stubLesson(languageId)}
        exercise={current.parsed}
        disabled={Boolean(result) || busy}
        selectedIndex={null}
        onAttempt={onAttempt}
        onHeardMs={(durationMs) => {
          void addListeningMs(userId, durationMs);
        }}
      />
      {result ? (
        <View accessibilityLabel={result.correct ? 'Correct' : 'Incorrect'}>
          <ThemedText type="defaultSemiBold">{result.correct ? 'Correct' : 'Incorrect'}</ThemedText>
          <ThemedText>{result.revealedAnswer}</ThemedText>
          <Pressable
            onPress={() => {
              if (index + 1 >= queue.length) {
                setComplete(true);
                return;
              }

              setIndex((value) => value + 1);
              setResult(null);
            }}
            accessibilityRole="button"
            accessibilityLabel="Continue">
            <ThemedText type="link">{index + 1 >= queue.length ? 'Finish' : 'Next'}</ThemedText>
          </Pressable>
        </View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
  },
});
