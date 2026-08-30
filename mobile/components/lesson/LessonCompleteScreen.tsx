import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

export type LessonCompleteStats = {
  title: string;
  xpReward: number;
  correctCount: number;
  totalCount: number;
  xpAwarded?: number;
  totalXp?: number;
  xpGranted?: boolean;
  currentStreak?: number;
};

export function LessonCompleteScreen({
  stats,
  onContinue,
  onPracticeAgain,
}: {
  stats: LessonCompleteStats;
  onContinue: () => void;
  onPracticeAgain: () => void;
}) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const accuracy = stats.totalCount === 0 ? 0 : Math.round((stats.correctCount / stats.totalCount) * 100);

  return (
    <View style={styles.container} accessibilityLabel="Lesson complete">
      <ThemedText type="title">Lesson complete</ThemedText>
      <ThemedText>{stats.title}</ThemedText>
      <ThemedText type="subtitle" accessibilityLabel={`Accuracy ${accuracy} percent`}>
        {accuracy}%
      </ThemedText>
      <ThemedText accessibilityLabel={`${stats.correctCount} of ${stats.totalCount} correct`}>
        {stats.correctCount}/{stats.totalCount} correct
      </ThemedText>
      {stats.xpReward > 0 ? (
        <ThemedText style={{ color: colors.icon }}>
          {stats.xpGranted === false
            ? `XP already awarded · ${stats.totalXp ?? stats.xpReward} total`
            : `+${stats.xpAwarded ?? stats.xpReward} XP${stats.totalXp != null ? ` · ${stats.totalXp} total` : ''}`}
        </ThemedText>
      ) : null}
      {stats.currentStreak != null ? (
        <ThemedText accessibilityLabel={`Streak ${stats.currentStreak} days`}>
          {stats.currentStreak} day streak
        </ThemedText>
      ) : null}
      <Pressable onPress={onContinue} accessibilityRole="button" accessibilityLabel="Continue">
        <ThemedText type="link">Continue</ThemedText>
      </Pressable>
      <Pressable onPress={onPracticeAgain} accessibilityRole="button" accessibilityLabel="Practice again">
        <ThemedText type="link">Practice again</ThemedText>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
    paddingVertical: 16,
  },
});
