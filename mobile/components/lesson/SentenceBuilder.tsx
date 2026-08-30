import { useMemo, useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';
import { Gesture, GestureDetector } from 'react-native-gesture-handler';
import { runOnJS } from 'react-native-reanimated';

import { ThemedText } from '@/components/themed-text';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

export function SentenceBuilder({
  prompt,
  tokens,
  disabled = false,
  onSubmit,
}: {
  prompt: string;
  tokens: readonly string[];
  disabled?: boolean;
  onSubmit: (order: readonly number[]) => void;
}) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const [order, setOrder] = useState<number[]>([]);

  const remaining = useMemo(
    () => tokens.map((_, index) => index).filter((index) => !order.includes(index)),
    [tokens, order],
  );

  const moveChosen = (from: number, to: number) => {
    setOrder((current) => {
      if (from < 0 || to < 0 || from >= current.length || to >= current.length) {
        return current;
      }

      const next = [...current];
      const [item] = next.splice(from, 1);
      if (item === undefined) {
        return current;
      }

      next.splice(to, 0, item);
      return next;
    });
  };

  return (
    <View style={styles.container}>
      {prompt ? <ThemedText type="subtitle">{prompt}</ThemedText> : null}
      <ThemedText>Build the sentence</ThemedText>
      <View style={[styles.row, styles.answer]} accessibilityLabel="Chosen words">
        {order.map((tokenIndex, position) => {
          const pan = Gesture.Pan()
            .enabled(!disabled)
            .onEnd((event) => {
              const delta = event.translationX;
              const target = delta > 24 ? position + 1 : delta < -24 ? position - 1 : position;
              runOnJS(moveChosen)(position, target);
            });

          return (
            <GestureDetector key={`chosen-${tokenIndex}`} gesture={pan}>
              <Pressable
                onPress={() => setOrder((current) => current.filter((index) => index !== tokenIndex))}
                disabled={disabled}
                accessibilityRole="button"
                accessibilityLabel={`Chosen token ${tokens[tokenIndex]}`}>
                <View style={[styles.token, { borderColor: colors.tint }]}>
                  <ThemedText>{tokens[tokenIndex]}</ThemedText>
                </View>
              </Pressable>
            </GestureDetector>
          );
        })}
      </View>
      <View style={styles.row} accessibilityLabel="Word bank">
        {remaining.map((tokenIndex) => (
          <Pressable
            key={`bank-${tokenIndex}`}
            onPress={() => setOrder((current) => [...current, tokenIndex])}
            disabled={disabled}
            accessibilityRole="button"
            accessibilityLabel={`Word token ${tokens[tokenIndex]}`}>
            <View style={[styles.token, { borderColor: colors.icon }]}>
              <ThemedText>{tokens[tokenIndex]}</ThemedText>
            </View>
          </Pressable>
        ))}
      </View>
      <Pressable
        onPress={() => onSubmit(order)}
        disabled={disabled}
        accessibilityRole="button"
        accessibilityLabel="Check sentence">
        <ThemedText type="link">Check</ThemedText>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
  },
  row: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    minHeight: 44,
  },
  answer: {
    borderBottomWidth: 1,
    borderBottomColor: '#687076',
    paddingBottom: 8,
  },
  token: {
    borderWidth: 1,
    borderRadius: 16,
    paddingHorizontal: 12,
    paddingVertical: 8,
  },
});
