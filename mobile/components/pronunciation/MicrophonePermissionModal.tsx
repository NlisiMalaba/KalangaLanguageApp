import { Modal, Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

export function MicrophonePermissionModal({
  visible,
  onOpenSettings,
  onDismiss,
}: {
  visible: boolean;
  onOpenSettings: () => void;
  onDismiss: () => void;
}) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];

  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onDismiss}>
      <View style={styles.backdrop} accessibilityViewIsModal>
        <View style={[styles.card, { backgroundColor: colors.background }]}>
          <ThemedText type="subtitle">Microphone access needed</ThemedText>
          <ThemedText>
            Pronunciation practice records your voice on this device only. Enable the microphone in
            Settings to continue.
          </ThemedText>
          <Pressable
            onPress={onOpenSettings}
            accessibilityRole="button"
            accessibilityLabel="Open settings"
            style={[styles.primary, { backgroundColor: colors.tint }]}>
            <ThemedText style={styles.primaryLabel}>Open settings</ThemedText>
          </Pressable>
          <Pressable onPress={onDismiss} accessibilityRole="button" accessibilityLabel="Not now">
            <ThemedText type="link">Not now</ThemedText>
          </Pressable>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    justifyContent: 'center',
    padding: 24,
    backgroundColor: 'rgba(0,0,0,0.45)',
  },
  card: {
    borderRadius: 16,
    padding: 20,
    gap: 12,
  },
  primary: {
    borderRadius: 12,
    paddingVertical: 12,
    alignItems: 'center',
  },
  primaryLabel: {
    color: '#fff',
    fontWeight: '600',
  },
});
