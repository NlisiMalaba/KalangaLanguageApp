import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import type { ReviewQueueItem } from '@/domain/review';

export function ReviewQueue({
  items,
  onApprove,
  busy = false,
}: {
  items: ReviewQueueItem[];
  onApprove: (lessonId: string) => void;
  busy?: boolean;
}) {
  return (
    <View style={styles.block}>
      <ThemedText type="title">Review queue</ThemedText>
      {items.length === 0 ? <ThemedText>No lessons are waiting for review.</ThemedText> : null}
      {items.map((item) => (
        <View key={item.lessonId} style={styles.card}>
          <ThemedText type="defaultSemiBold">{item.title}</ThemedText>
          <ThemedText>
            {item.level} · {item.category}
          </ThemedText>
          <Pressable
            onPress={() => onApprove(item.lessonId)}
            disabled={busy}
            accessibilityRole="button"
            accessibilityLabel={`Approve ${item.title}`}>
            <ThemedText type="link">Approve</ThemedText>
          </Pressable>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  block: {
    gap: 12,
  },
  card: {
    gap: 6,
    padding: 12,
    borderWidth: 1,
    borderColor: '#d0d5d8',
    borderRadius: 12,
  },
});
