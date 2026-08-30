import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import type { PronunciationScore } from '@/domain/pronunciation/types';

export function FeedbackView({ score }: { score: PronunciationScore }) {
  return (
    <View style={styles.container} accessibilityLabel={`Pronunciation score ${score.score}. ${score.label}.`}>
      <ThemedText type="title" accessibilityLabel={`Score ${score.score}`}>
        {score.score}
      </ThemedText>
      <ThemedText type="subtitle" accessibilityLabel={`Feedback ${score.label}`}>
        {score.label}
      </ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    gap: 4,
    paddingVertical: 12,
  },
});
