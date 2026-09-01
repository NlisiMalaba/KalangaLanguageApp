import { Pressable, StyleSheet, View } from 'react-native';

import { LessonContent } from '@/components/lesson/LessonContent';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import type { LessonDetail } from '@/domain/catalog/types';
import type { EntityId } from '@/domain/entities';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

export function ConversationView({
  lesson,
  onPractisePhrase,
  onStartExercises,
}: {
  lesson: LessonDetail;
  onPractisePhrase?: (phraseId: EntityId) => void;
  onStartExercises?: () => void;
}) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const situation = lesson.scenarioContext?.trim() || 'No situation description was provided.';

  return (
    <ThemedView style={styles.container}>
      <ThemedText type="title">{lesson.title}</ThemedText>
      <ThemedText style={{ color: colors.icon }}>
        {lesson.level} · {lesson.category} · {lesson.xpReward} XP
      </ThemedText>
      <View style={[styles.situation, { borderColor: colors.icon }]} accessibilityLabel="Situation context">
        <ThemedText type="defaultSemiBold">Situation</ThemedText>
        <ThemedText>{situation}</ThemedText>
      </View>
      <LessonContent
        lesson={lesson}
        hideTitle
        hideScenario
        onPractisePhrase={onPractisePhrase}
        onStartExercises={onStartExercises}
      />
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
    paddingBottom: 24,
  },
  situation: {
    borderWidth: 1,
    borderRadius: 12,
    padding: 12,
    gap: 6,
  },
});
