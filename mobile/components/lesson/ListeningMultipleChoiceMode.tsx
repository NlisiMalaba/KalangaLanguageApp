import { AudioPrompt } from '@/components/lesson/AudioPrompt';
import type { AudioPlayerFactory } from '@/components/lesson/AudioPrompt';
import { MultipleChoiceMode } from '@/components/lesson/MultipleChoiceMode';
import { ThemedText } from '@/components/themed-text';
import type { AudioRef } from '@/domain/catalog/types';
import type { EntityId } from '@/domain/entities';
import { View, StyleSheet } from 'react-native';

export function ListeningMultipleChoiceMode({
  languageId,
  recordings,
  ttsText,
  options,
  selectedIndex,
  disabled = false,
  onSelect,
  createPlayer,
}: {
  languageId: EntityId;
  recordings: readonly AudioRef[];
  ttsText: string;
  options: readonly string[];
  selectedIndex: number | null;
  disabled?: boolean;
  onSelect: (index: number) => void;
  createPlayer?: AudioPlayerFactory;
}) {
  return (
    <View style={styles.container}>
      <ThemedText type="subtitle">What did you hear?</ThemedText>
      <AudioPrompt
        languageId={languageId}
        recordings={recordings}
        ttsText={ttsText}
        createPlayer={createPlayer}
      />
      <MultipleChoiceMode
        prompt={undefined}
        options={options}
        selectedIndex={selectedIndex}
        disabled={disabled}
        onSelect={onSelect}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
  },
});
