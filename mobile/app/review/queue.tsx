import { useCallback, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet } from 'react-native';
import { useFocusEffect } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';
import { toast } from 'sonner-native';

import { ReviewQueue } from '@/components/review/ReviewQueue';
import { ThemedText } from '@/components/themed-text';
import { getLanguageId } from '@/constants/config';
import { useAuth } from '@/ctx/AuthContext';
import { Role } from '@/domain/enums';
import {
  ReviewError,
  createApproveLessonUseCase,
  createListReviewQueueUseCase,
  type ReviewQueueItem,
} from '@/domain/review';
import { createHttpReviewApi } from '@/lib/review/httpReviewApi';

const defaultApi = createHttpReviewApi();
const defaultList = createListReviewQueueUseCase(defaultApi);
const defaultApprove = createApproveLessonUseCase(defaultApi);

export default function ReviewQueueScreen({
  listQueue = defaultList,
  approveLesson = defaultApprove,
}: {
  listQueue?: typeof defaultList;
  approveLesson?: typeof defaultApprove;
}) {
  const { user } = useAuth();
  const canReview = user?.role === Role.Reviewer || user?.role === Role.Admin;
  const [items, setItems] = useState<ReviewQueueItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    if (!user || !canReview) {
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      setItems(await listQueue(getLanguageId()));
    } catch (error) {
      const message = error instanceof ReviewError ? error.message : 'Could not load the review queue.';
      toast.error(message);
    } finally {
      setLoading(false);
    }
  }, [canReview, listQueue, user]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  const onApprove = async (lessonId: string) => {
    setBusy(true);
    try {
      await approveLesson(getLanguageId(), lessonId);
      setItems(items.filter((item) => item.lessonId !== lessonId));
      toast.success('Lesson published.');
    } catch (error) {
      const message = error instanceof ReviewError ? error.message : 'Could not approve that lesson.';
      toast.error(message);
    } finally {
      setBusy(false);
    }
  };

  if (!user || !canReview) {
    return (
      <SafeAreaView style={styles.safe} edges={['top']}>
        <ThemedText>Reviewer access is required.</ThemedText>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <ScrollView contentContainerStyle={styles.padded}>
        {loading ? <ActivityIndicator /> : null}
        <ReviewQueue items={items} onApprove={(lessonId) => void onApprove(lessonId)} busy={busy} />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
  },
  padded: {
    padding: 24,
  },
});
