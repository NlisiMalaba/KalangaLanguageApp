import { useCallback, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet } from 'react-native';
import { router, useFocusEffect, useLocalSearchParams } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';
import { toast } from 'sonner-native';

import { ConversationView } from '@/components/conversation/ConversationView';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { getLanguageId } from '@/constants/config';
import { CatalogError, LessonNotFoundError } from '@/domain/catalog/errors';
import type { LessonDetail } from '@/domain/catalog/types';
import { createDefaultGetLessonUseCase } from '@/lib/catalog/createBrowseLessonCatalogUseCase';

const getLesson = createDefaultGetLessonUseCase();

export default function ConversationScreen({
  loadLesson = getLesson,
}: {
  loadLesson?: typeof getLesson;
}) {
  const { id } = useLocalSearchParams<{ id: string }>();
  const [lesson, setLesson] = useState<LessonDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useFocusEffect(
    useCallback(() => {
      const lessonId = typeof id === 'string' ? id : '';
      if (!lessonId) {
        setError('This conversation is not available.');
        setLoading(false);
        return;
      }

      let cancelled = false;
      setLoading(true);
      setError(null);

      void loadLesson({ languageId: getLanguageId(), lessonId })
        .then((detail) => {
          if (!cancelled) {
            setLesson(detail);
          }
        })
        .catch((caught) => {
          const message =
            caught instanceof LessonNotFoundError
              ? caught.message
              : caught instanceof CatalogError
                ? caught.message
                : 'Could not load this conversation.';
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
    }, [id, loadLesson]),
  );

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <ScrollView contentContainerStyle={styles.scroll}>
        {loading ? <ActivityIndicator /> : null}
        {error ? (
          <ThemedView>
            <ThemedText>{error}</ThemedText>
          </ThemedView>
        ) : null}
        {lesson ? (
          <ConversationView
            lesson={lesson}
            onPractisePhrase={(phraseId) =>
              router.push({ pathname: '/practise', params: { lessonId: lesson.id, phraseId } })
            }
            onStartExercises={() =>
              router.push({ pathname: '/practise', params: { lessonId: lesson.id, mode: 'exercises' } })
            }
          />
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
});
