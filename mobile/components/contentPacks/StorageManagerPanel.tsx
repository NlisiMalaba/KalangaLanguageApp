import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { Colors } from '@/constants/theme';
import { formatStorageBytes, type StorageSummary } from '@/domain/contentPacks/storage';
import type { EntityId } from '@/domain/entities';
import { useColorScheme } from '@/hooks/use-color-scheme';

export function StorageManagerPanel({
  summary,
  deletingPackId,
  onDeletePack,
}: {
  summary: StorageSummary;
  deletingPackId?: EntityId | null;
  onDeletePack: (packId: EntityId) => void;
}) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const totalLabel = formatStorageBytes(summary.totalBytes);

  return (
    <View style={styles.section}>
      <ThemedText type="subtitle">Storage</ThemedText>
      <ThemedText accessibilityLabel={`Total storage ${totalLabel}`}>
        {totalLabel} used by downloaded packs
      </ThemedText>
      {summary.packs.length === 0 ? (
        <ThemedText>No packs are stored on this device yet.</ThemedText>
      ) : null}
      {summary.packs.map((pack) => (
        <View key={pack.packId} style={styles.row}>
          <ThemedText type="defaultSemiBold">{pack.name}</ThemedText>
          <ThemedText style={{ color: colors.icon }}>{formatStorageBytes(pack.sizeBytes)}</ThemedText>
          {pack.updateAvailable ? (
            <ThemedText accessibilityLabel={`${pack.name} update available`}>
              Update available (v{pack.remoteVersion})
            </ThemedText>
          ) : null}
          <Pressable
            onPress={() => onDeletePack(pack.packId)}
            disabled={Boolean(deletingPackId)}
            accessibilityRole="button"
            accessibilityLabel={`Delete ${pack.name}`}
            accessibilityState={{ disabled: Boolean(deletingPackId), busy: deletingPackId === pack.packId }}>
            <ThemedText type="link">
              {deletingPackId === pack.packId ? 'Deleting…' : 'Delete'}
            </ThemedText>
          </Pressable>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    gap: 8,
  },
  row: {
    gap: 4,
  },
});
