import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { LessonDownloadStatus, type CatalogLessonItem } from '@/domain/catalog/types';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

function downloadLabel(status: LessonDownloadStatus): string {
  if (status === LessonDownloadStatus.Downloaded) {
    return 'Downloaded';
  }

  if (status === LessonDownloadStatus.Partial) {
    return 'Downloading';
  }

  return 'Download required';
}

export function LessonCard({
  lesson,
  onPress,
}: {
  lesson: CatalogLessonItem;
  onPress: (lesson: CatalogLessonItem) => void;
}) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const status = downloadLabel(lesson.downloadStatus);
  const completion = lesson.isCompleted ? 'Completed' : 'Not completed';

  return (
    <Pressable
      onPress={() => onPress(lesson)}
      accessibilityRole="button"
      accessibilityLabel={`${lesson.title}. ${lesson.category}. ${completion}. ${status}. ${lesson.xpReward} XP.`}
      style={[styles.card, { borderColor: colors.icon, backgroundColor: colors.background }]}>
      <View style={styles.header}>
        <ThemedText type="defaultSemiBold" style={styles.title}>
          {lesson.title}
        </ThemedText>
        {lesson.isCompleted ? (
          <View style={styles.badge} accessibilityLabel="Completed">
            <ThemedText style={styles.badgeText}>Completed</ThemedText>
          </View>
        ) : null}
      </View>
      <ThemedText style={{ color: colors.icon }}>
        {lesson.category} · {lesson.xpReward} XP
      </ThemedText>
      <ThemedText style={{ color: colors.icon }}>{status}</ThemedText>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  card: {
    borderWidth: 1,
    borderRadius: 16,
    padding: 16,
    gap: 6,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 8,
  },
  title: {
    flex: 1,
  },
  badge: {
    backgroundColor: '#0a7ea4',
    borderRadius: 12,
    paddingHorizontal: 8,
    paddingVertical: 4,
  },
  badgeText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '600',
  },
});
