import { StyleSheet, View } from 'react-native';

import { AudioPrompt } from '@/components/lesson/AudioPrompt';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { recordingsForPhrase } from '@/domain/audio/phraseRecordings';
import type { LessonDetail } from '@/domain/catalog/types';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

export function LessonContent({ lesson }: { lesson: LessonDetail }) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];

  return (
    <ThemedView style={styles.container}>
      <ThemedText type="title">{lesson.title}</ThemedText>
      <ThemedText style={{ color: colors.icon }}>
        {lesson.level} · {lesson.category} · {lesson.xpReward} XP
      </ThemedText>

      {lesson.isScenario && lesson.scenarioContext ? (
        <View style={[styles.scenario, { borderColor: colors.icon }]} accessibilityLabel="Scenario context">
          <ThemedText type="defaultSemiBold">Scenario</ThemedText>
          <ThemedText>{lesson.scenarioContext}</ThemedText>
        </View>
      ) : null}

      <ThemedText type="subtitle">Phrases</ThemedText>
      {lesson.phrases.length === 0 ? <ThemedText>No phrases in this lesson yet.</ThemedText> : null}
      {lesson.phrases.map((phrase) => (
        <View key={phrase.id} style={[styles.block, { borderColor: colors.icon }]}>
          <ThemedText type="defaultSemiBold">{phrase.kalangaText}</ThemedText>
          <ThemedText>{phrase.englishTranslation}</ThemedText>
          <AudioPrompt
            languageId={lesson.languageId}
            recordings={recordingsForPhrase(phrase)}
            ttsText={phrase.kalangaText}
          />
          {phrase.variations.map((variation) => (
            <View key={variation.id} style={styles.variation}>
              <ThemedText type="defaultSemiBold">{variation.registerLabel}</ThemedText>
              <ThemedText>{variation.kalangaText}</ThemedText>
            </View>
          ))}
        </View>
      ))}

      <ThemedText type="subtitle">Exercises</ThemedText>
      {lesson.exercises.length === 0 ? <ThemedText>No exercises in this lesson yet.</ThemedText> : null}
      {lesson.exercises.map((exercise) => (
        <View key={exercise.id} style={[styles.block, { borderColor: colors.icon }]}>
          <ThemedText type="defaultSemiBold">{exercise.exerciseType}</ThemedText>
          <ThemedText>{exercise.promptData}</ThemedText>
        </View>
      ))}
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
    paddingBottom: 48,
  },
  scenario: {
    borderWidth: 1,
    borderRadius: 12,
    padding: 12,
    gap: 6,
  },
  block: {
    borderWidth: 1,
    borderRadius: 12,
    padding: 12,
    gap: 6,
  },
  variation: {
    marginTop: 8,
    gap: 4,
    paddingLeft: 8,
    borderLeftWidth: 2,
    borderLeftColor: '#0a7ea4',
  },
});
