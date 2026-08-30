import { useCallback, useState } from 'react';
import { Modal, Pressable, StyleSheet, View } from 'react-native';
import { toast } from 'sonner-native';

import { ThemedText } from '@/components/themed-text';
import { Colors } from '@/constants/theme';
import type { ContentPackListItem, DownloadContentPackResult, PackDownloadProgress } from '@/domain/contentPacks/types';
import { ContentPackError } from '@/domain/contentPacks/errors';
import type { EntityId } from '@/domain/entities';
import { useColorScheme } from '@/hooks/use-color-scheme';

export function ContentPackDownloadPanel({
  languageId,
  packs,
  downloadPack,
}: {
  languageId: EntityId;
  packs: ContentPackListItem[];
  downloadPack: (input: {
    languageId: EntityId;
    packId: EntityId;
    onProgress?: (progress: PackDownloadProgress) => void;
  }) => Promise<DownloadContentPackResult>;
}) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const [activePackId, setActivePackId] = useState<EntityId | null>(null);
  const [percentByPack, setPercentByPack] = useState<Record<string, number>>({});
  const [storageWarning, setStorageWarning] = useState(false);

  const onDownload = useCallback(
    async (packId: EntityId) => {
      setActivePackId(packId);
      try {
        const result = await downloadPack({
          languageId,
          packId,
          onProgress: (snapshot) => {
            setPercentByPack((current) => ({ ...current, [packId]: snapshot.percent }));
          },
        });
        setPercentByPack((current) => ({ ...current, [packId]: result.percent }));
        if (result.pausedForStorage) {
          setStorageWarning(true);
        }
      } catch (error) {
        const message =
          error instanceof ContentPackError ? error.message : 'Could not download this content pack.';
        toast.error(message);
      } finally {
        setActivePackId(null);
      }
    },
    [downloadPack, languageId],
  );

  return (
    <View style={styles.section}>
      <ThemedText type="subtitle">Offline packs</ThemedText>
      {packs.length === 0 ? <ThemedText>No content packs are available yet.</ThemedText> : null}
      {packs.map((pack) => {
        const percent = percentByPack[pack.packId];
        const busy = activePackId === pack.packId;
        return (
          <View key={pack.packId} style={styles.row}>
            <ThemedText type="defaultSemiBold">{pack.name}</ThemedText>
            <ThemedText style={{ color: colors.icon }}>
              {pack.level ?? 'All levels'}
              {pack.category ? ` · ${pack.category}` : ''}
            </ThemedText>
            {percent != null ? (
              <ThemedText accessibilityLabel={`Download ${percent} percent`}>{percent}%</ThemedText>
            ) : null}
            <Pressable
              onPress={() => void onDownload(pack.packId)}
              disabled={busy || activePackId != null}
              accessibilityRole="button"
              accessibilityLabel={`Download ${pack.name}`}
              accessibilityState={{ disabled: busy || activePackId != null, busy }}>
              <ThemedText type="link">{busy ? 'Downloading…' : 'Download'}</ThemedText>
            </Pressable>
          </View>
        );
      })}

      <Modal
        visible={storageWarning}
        transparent
        animationType="fade"
        onRequestClose={() => setStorageWarning(false)}>
        <View style={styles.backdrop} accessibilityViewIsModal>
          <View style={[styles.card, { backgroundColor: colors.background }]}>
            <ThemedText type="subtitle">Storage almost full</ThemedText>
            <ThemedText>
              Download paused because this device has less than 50 MB free. Free some space and try
              again.
            </ThemedText>
            <Pressable
              onPress={() => setStorageWarning(false)}
              accessibilityRole="button"
              accessibilityLabel="Dismiss storage warning">
              <ThemedText type="link">OK</ThemedText>
            </Pressable>
          </View>
        </View>
      </Modal>
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
});
