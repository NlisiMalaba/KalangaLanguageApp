import { useState } from 'react';
import { Pressable, StyleSheet, TextInput, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

export function Flashcard({
  kalangaText,
  disabled = false,
  onSubmitTranslation,
  onSelfGrade,
}: {
  kalangaText: string;
  disabled?: boolean;
  onSubmitTranslation: (translation: string) => void;
  onSelfGrade?: (remembered: boolean) => void;
}) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const [translation, setTranslation] = useState('');
  const [revealed, setRevealed] = useState(false);

  return (
    <View style={styles.container}>
      <ThemedText type="subtitle">Recall the translation</ThemedText>
      <ThemedText type="title" accessibilityLabel={`Phrase ${kalangaText}`}>
        {kalangaText}
      </ThemedText>
      <TextInput
        value={translation}
        onChangeText={setTranslation}
        editable={!disabled}
        accessibilityLabel="Translation"
        placeholder="English"
        placeholderTextColor={colors.icon}
        style={[styles.input, { color: colors.text, borderColor: colors.icon }]}
      />
      <Pressable
        onPress={() => onSubmitTranslation(translation)}
        disabled={disabled}
        accessibilityRole="button"
        accessibilityLabel="Check translation">
        <ThemedText type="link">Check</ThemedText>
      </Pressable>
      {onSelfGrade ? (
        <>
          <Pressable
            onPress={() => {
              setRevealed(true);
            }}
            disabled={disabled}
            accessibilityRole="button"
            accessibilityLabel="Reveal translation">
            <ThemedText type="link">Reveal translation</ThemedText>
          </Pressable>
          {revealed ? (
            <View style={styles.selfGrade}>
              <Pressable
                onPress={() => onSelfGrade(true)}
                disabled={disabled}
                accessibilityRole="button"
                accessibilityLabel="I remembered">
                <ThemedText type="link">I remembered</ThemedText>
              </Pressable>
              <Pressable
                onPress={() => onSelfGrade(false)}
                disabled={disabled}
                accessibilityRole="button"
                accessibilityLabel="Need review">
                <ThemedText type="link">Need review</ThemedText>
              </Pressable>
            </View>
          ) : null}
        </>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
  },
  input: {
    borderWidth: 1,
    borderRadius: 12,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
  },
  selfGrade: {
    flexDirection: 'row',
    gap: 16,
  },
});
