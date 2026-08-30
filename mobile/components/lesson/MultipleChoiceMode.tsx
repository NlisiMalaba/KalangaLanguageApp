import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

export function MultipleChoiceMode({
  prompt,
  options,
  selectedIndex,
  disabled = false,
  onSelect,
}: {
  prompt?: string;
  options: readonly string[];
  selectedIndex: number | null;
  disabled?: boolean;
  onSelect: (index: number) => void;
}) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];

  return (
    <View style={styles.container}>
      {prompt ? <ThemedText type="subtitle">{prompt}</ThemedText> : null}
      {options.map((option, index) => {
        const selected = selectedIndex === index;
        return (
          <Pressable
            key={`${index}-${option}`}
            onPress={() => onSelect(index)}
            disabled={disabled}
            accessibilityRole="button"
            accessibilityState={{ selected, disabled }}
            accessibilityLabel={`Option ${index + 1}: ${option}`}
            style={[
              styles.option,
              { borderColor: selected ? colors.tint : colors.icon },
              selected && { backgroundColor: colors.tint },
            ]}>
            <ThemedText style={selected ? styles.selected : undefined}>{option}</ThemedText>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 10,
  },
  option: {
    borderWidth: 1,
    borderRadius: 12,
    padding: 14,
  },
  selected: {
    color: '#fff',
    fontWeight: '600',
  },
});
