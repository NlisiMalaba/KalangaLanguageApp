import { useCallback, useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, View } from 'react-native';
import { router, useFocusEffect, useLocalSearchParams } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';
import { toast } from 'sonner-native';

import { DailyReviewSequence } from '@/components/lesson/DailyReviewSequence';
import { ExerciseSequence } from '@/components/lesson/ExerciseSequence';
import { PronunciationPractice } from '@/components/pronunciation/PronunciationPractice';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { getLanguageId } from '@/constants/config';
import { Colors } from '@/constants/theme';
import { useAuth } from '@/ctx/AuthContext';
import { recordingsForPhrase } from '@/domain/audio/phraseRecordings';
import { CatalogError, LessonNotFoundError } from '@/domain/catalog/errors';
import type { LessonDetail } from '@/domain/catalog/types';
import type { ExerciseEngine } from '@/domain/exercises/exerciseEngine';
import type { CompleteLessonInput, CompleteLessonResult } from '@/domain/progress/completeLessonUseCase';
import type { ReviewPrompt } from '@/domain/progress/dailyReview';
import type { SpacedRepetitionEngine } from '@/domain/srs/spacedRepetitionEngine';
import { createDefaultGetLessonUseCase } from '@/lib/catalog/createBrowseLessonCatalogUseCase';
import { createDefaultCompleteLessonUseCase } from '@/lib/createCompleteLessonUseCase';
import { createLoadReviewPrompts } from '@/lib/createLoadReviewPrompts';
import { createDefaultExerciseEngine } from '@/lib/createExerciseEngine';
import { createDefaultSpacedRepetitionEngine } from '@/lib/spacedRepetitionEngine';
import { createDefaultPronunciationRecorder } from '@/lib/pronunciation/createPronunciationRecorder';
import { useColorScheme } from '@/hooks/use-color-scheme';

const defaultGetLesson = createDefaultGetLessonUseCase();
const defaultCreateRecorder = () => createDefaultPronunciationRecorder();
const defaultEngine = createDefaultExerciseEngine();
const defaultCompleteLesson = createDefaultCompleteLessonUseCase();
const defaultSrs = createDefaultSpacedRepetitionEngine();
const defaultLoadReview = createLoadReviewPrompts(defaultSrs);

export type PractiseScreenDeps = {
  getLesson?: typeof defaultGetLesson;
  createRecorder?: typeof defaultCreateRecorder;
  exerciseEngine?: ExerciseEngine;
  completeLesson?: (input: CompleteLessonInput) => Promise<CompleteLessonResult>;
  srs?: SpacedRepetitionEngine;
  loadReviewPrompts?: (input: { languageId: string; userId: string }) => Promise<ReviewPrompt[]>;
};

export default function PractiseScreen({
  getLesson = defaultGetLesson,
  createRecorder = defaultCreateRecorder,
  exerciseEngine = defaultEngine,
  completeLesson = defaultCompleteLesson,
  srs = defaultSrs,
  loadReviewPrompts = defaultLoadReview,
}: PractiseScreenDeps) {
  const { user } = useAuth();
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const { lessonId, phraseId, mode } = useLocalSearchParams<{
    lessonId?: string;
    phraseId?: string;
    mode?: string;
  }>();
  const exercisesMode = mode === 'exercises';
  const reviewMode = mode === 'review';
  const [lesson, setLesson] = useState<LessonDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(Boolean(lessonId) && !reviewMode);
  const [activePhraseId, setActivePhraseId] = useState<string | null>(
    typeof phraseId === 'string' ? phraseId : null,
  );

  useFocusEffect(
    useCallback(() => {
      const id = typeof lessonId === 'string' ? lessonId : '';
      if (!id || reviewMode) {
        setLesson(null);
        setLoading(false);
        setError(null);
        return;
      }

      let cancelled = false;
      setLoading(true);
      setError(null);

      void getLesson({ languageId: getLanguageId(), lessonId: id })
        .then((detail) => {
          if (cancelled) {
            return;
          }

          setLesson(detail);
          const requested = typeof phraseId === 'string' ? phraseId : null;
          const first = detail.phrases[0]?.id ?? null;
          setActivePhraseId(
            requested && detail.phrases.some((phrase) => phrase.id === requested) ? requested : first,
          );
        })
        .catch((caught) => {
          const message =
            caught instanceof LessonNotFoundError
              ? caught.message
              : caught instanceof CatalogError
                ? caught.message
                : 'Could not load this lesson.';
          if (!cancelled) {
            setLesson(null);
            setError(message);
            toast.error(message);
          }
        })
        .finally(() => {
          if (!cancelled) {
            setLoading(false);
          }
        });

      return () => {
        cancelled = true;
      };
    }, [getLesson, lessonId, phraseId, reviewMode]),
  );

  const phraseIndex = useMemo(() => {
    if (!lesson || !activePhraseId) {
      return -1;
    }

    return lesson.phrases.findIndex((item) => item.id === activePhraseId);
  }, [lesson, activePhraseId]);

  const phrase = phraseIndex >= 0 ? lesson?.phrases[phraseIndex] ?? null : null;

  if (!lessonId && !reviewMode) {
    return (
      <SafeAreaView style={styles.safe} edges={['bottom']}>
        <ThemedView style={styles.empty}>
          <ThemedText type="title">Practise</ThemedText>
          <ThemedText>
            Open a lesson to practise pronunciation or start its exercises. Use Daily review for due
            phrases. Recordings stay on this device unless you consent to upload.
          </ThemedText>
        </ThemedView>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <ScrollView contentContainerStyle={styles.scroll}>
        {loading ? <ActivityIndicator /> : null}
        {error ? (
          <ThemedView>
            <ThemedText>{error}</ThemedText>
          </ThemedView>
        ) : null}
        {reviewMode ? (
          user ? (
            <DailyReviewSequence
              languageId={getLanguageId()}
              userId={user.id}
              loadPrompts={() =>
                loadReviewPrompts({ languageId: getLanguageId(), userId: user.id })
              }
              srs={srs}
              onContinue={() => router.back()}
            />
          ) : (
            <ThemedText>Sign in to review due phrases.</ThemedText>
          )
        ) : null}
        {lesson && exercisesMode ? (
          user ? (
            <ExerciseSequence
              lesson={lesson}
              userId={user.id}
              engine={exerciseEngine}
              onContinue={() => router.back()}
              onLessonComplete={({ score }) =>
                completeLesson({
                  languageId: lesson.languageId,
                  userId: user.id,
                  lessonId: lesson.id,
                  score,
                  xpReward: lesson.xpReward,
                })
              }
            />
          ) : (
            <ThemedText>Sign in to practise exercises.</ThemedText>
          )
        ) : null}
        {lesson && phrase && !exercisesMode ? (
          <View style={styles.session}>
            <ThemedText style={{ color: colors.icon }}>
              {lesson.title} · {phraseIndex + 1} of {lesson.phrases.length}
            </ThemedText>
            <PronunciationPractice
              languageId={lesson.languageId}
              phrase={phrase}
              recordings={recordingsForPhrase(phrase)}
              createRecorder={createRecorder}
            />
            {lesson.phrases.length > 1 ? (
              <View style={styles.nav}>
                <Pressable
                  onPress={() => setActivePhraseId(lesson.phrases[phraseIndex - 1]?.id ?? phrase.id)}
                  disabled={phraseIndex <= 0}
                  accessibilityRole="button"
                  accessibilityLabel="Previous phrase"
                  accessibilityState={{ disabled: phraseIndex <= 0 }}>
                  <ThemedText type={phraseIndex <= 0 ? 'default' : 'link'}>Previous</ThemedText>
                </Pressable>
                <Pressable
                  onPress={() => setActivePhraseId(lesson.phrases[phraseIndex + 1]?.id ?? phrase.id)}
                  disabled={phraseIndex >= lesson.phrases.length - 1}
                  accessibilityRole="button"
                  accessibilityLabel="Next phrase"
                  accessibilityState={{ disabled: phraseIndex >= lesson.phrases.length - 1 }}>
                  <ThemedText type={phraseIndex >= lesson.phrases.length - 1 ? 'default' : 'link'}>
                    Next
                  </ThemedText>
                </Pressable>
              </View>
            ) : null}
          </View>
        ) : null}
        {lesson && !loading && lesson.phrases.length === 0 && !exercisesMode ? (
          <ThemedText>This lesson has no phrases to practise yet.</ThemedText>
        ) : null}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
  },
  scroll: {
    padding: 24,
    gap: 16,
  },
  empty: {
    flex: 1,
    padding: 24,
    gap: 12,
  },
  session: {
    gap: 16,
  },
  nav: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
});
