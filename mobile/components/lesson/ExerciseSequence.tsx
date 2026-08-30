import { useCallback, useEffect, useRef, useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';
import { toast } from 'sonner-native';

import { ExercisePlayer } from '@/components/lesson/ExercisePlayer';
import { LessonCompleteScreen } from '@/components/lesson/LessonCompleteScreen';
import { ThemedText } from '@/components/themed-text';
import type { LessonDetail } from '@/domain/catalog/types';
import type { EntityId } from '@/domain/entities';
import { ExerciseType } from '@/domain/enums';
import { ExerciseError } from '@/domain/exercises/errors';
import type { ExerciseEngine } from '@/domain/exercises/exerciseEngine';
import {
  exerciseSourcesFromLesson,
  prepareExercises,
  type PreparedExercise,
} from '@/domain/exercises/prepareExercises';
import type { CompleteLessonResult } from '@/domain/progress/completeLessonUseCase';
import type { ExerciseAttempt, GradeResult } from '@/domain/exercises/types';

export function ExerciseSequence({
  lesson,
  userId,
  engine,
  onContinue,
  onLessonComplete,
}: {
  lesson: LessonDetail;
  userId: EntityId;
  engine: ExerciseEngine;
  onContinue: () => void;
  onLessonComplete?: (input: {
    score: number;
    correctCount: number;
    totalCount: number;
  }) => Promise<CompleteLessonResult>;
}) {
  const fallback = prepareExercises(exerciseSourcesFromLesson(lesson));
  const [queue, setQueue] = useState<PreparedExercise[]>(fallback);
  const [index, setIndex] = useState(0);
  const [result, setResult] = useState<GradeResult | null>(null);
  const [correctCount, setCorrectCount] = useState(0);
  const [complete, setComplete] = useState(false);
  const [completion, setCompletion] = useState<CompleteLessonResult | null>(null);
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const [busy, setBusy] = useState(false);
  const correctCountRef = useRef(0);

  const reset = useCallback(() => {
    setIndex(0);
    setResult(null);
    setCorrectCount(0);
    correctCountRef.current = 0;
    setComplete(false);
    setCompletion(null);
    setSelectedIndex(null);
  }, []);

  useEffect(() => {
    let cancelled = false;
    void engine
      .loadLessonExercises(lesson.languageId, lesson.id)
      .then((loaded) => {
        if (!cancelled && loaded.length > 0) {
          setQueue(loaded);
          reset();
        }
      })
      .catch(() => undefined);

    return () => {
      cancelled = true;
    };
  }, [engine, lesson.id, lesson.languageId, reset]);

  const current = queue[index] ?? null;

  const onAttempt = (attempt: ExerciseAttempt) => {
    if (!current || result || busy) {
      return;
    }

    if (attempt.kind === ExerciseType.MultipleChoice || attempt.kind === ExerciseType.Listening) {
      setSelectedIndex(attempt.selectedIndex);
    }

    setBusy(true);
    void engine
      .submit({
        languageId: lesson.languageId,
        userId,
        exercise: current.source,
        attempt,
      })
      .then((grade) => {
        setResult(grade);
        if (grade.correct) {
          correctCountRef.current += 1;
          setCorrectCount(correctCountRef.current);
        }
      })
      .catch((error) => {
        const message = error instanceof ExerciseError ? error.message : 'Could not score that answer.';
        toast.error(message);
      })
      .finally(() => {
        setBusy(false);
      });
  };

  const onNext = () => {
    if (index + 1 >= queue.length) {
      const totalCount = queue.length;
      const score = totalCount === 0 ? 0 : Math.round((correctCountRef.current / totalCount) * 100);
      if (!onLessonComplete) {
        setComplete(true);
        return;
      }

      setBusy(true);
      void onLessonComplete({ score, correctCount: correctCountRef.current, totalCount })
        .then((result) => {
          setCompletion(result);
          setComplete(true);
        })
        .catch((error) => {
          const message = error instanceof Error ? error.message : 'Could not save lesson progress.';
          toast.error(message);
          setComplete(true);
        })
        .finally(() => {
          setBusy(false);
        });
      return;
    }

    setIndex((value) => value + 1);
    setResult(null);
    setSelectedIndex(null);
  };

  if (queue.length === 0) {
    return <ThemedText>This lesson has no valid exercises yet.</ThemedText>;
  }

  if (complete) {
    return (
      <LessonCompleteScreen
        stats={{
          title: lesson.title,
          xpReward: lesson.xpReward,
          correctCount,
          totalCount: queue.length,
          xpAwarded: completion?.xpAwarded,
          totalXp: completion?.totalXp,
          xpGranted: completion?.xpGranted,
          currentStreak: completion?.currentStreak,
        }}
        onContinue={onContinue}
        onPracticeAgain={reset}
      />
    );
  }

  if (!current) {
    return null;
  }

  return (
    <View style={styles.container}>
      <ThemedText accessibilityLabel={`Exercise ${index + 1} of ${queue.length}`}>
        Exercise {index + 1} of {queue.length}
      </ThemedText>
      <ExercisePlayer
        key={current.source.id}
        lesson={lesson}
        exercise={current.parsed}
        disabled={Boolean(result) || busy}
        selectedIndex={selectedIndex}
        onAttempt={onAttempt}
      />
      {result ? (
        <View accessibilityLabel={result.correct ? 'Correct' : 'Incorrect'}>
          <ThemedText type="defaultSemiBold">{result.correct ? 'Correct' : 'Incorrect'}</ThemedText>
          <ThemedText accessibilityLabel={`Answer ${result.revealedAnswer}`}>
            {result.revealedAnswer}
          </ThemedText>
          <Pressable onPress={onNext} accessibilityRole="button" accessibilityLabel="Continue">
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
