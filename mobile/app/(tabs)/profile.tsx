import { useCallback, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, View } from 'react-native';
import { useFocusEffect } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';
import { toast } from 'sonner-native';

import { ContentPackDownloadPanel } from '@/components/contentPacks/ContentPackDownloadPanel';
import { StorageManagerPanel } from '@/components/contentPacks/StorageManagerPanel';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { getLanguageId } from '@/constants/config';
import { Colors } from '@/constants/theme';
import { useAuth } from '@/ctx/AuthContext';
import type { GetProgressResult } from '@/domain/progress/types';
import type { StorageSummary } from '@/domain/contentPacks/storage';
import type { ContentPackListItem, DownloadContentPackResult, PackDownloadProgress } from '@/domain/contentPacks/types';
import { ContentPackError } from '@/domain/contentPacks/errors';
import { useColorScheme } from '@/hooks/use-color-scheme';
import { useSpeakingListeningStats } from '@/hooks/useSpeakingListeningStats';
import { createDefaultGetProgressUseCase } from '@/lib/createGetProgressUseCase';
import { createDefaultContentPackDownloader } from '@/lib/contentPacks/createContentPackDownloader';
import { createDefaultStorageManager } from '@/lib/contentPacks/createStorageManager';
import { createHttpContentPackApi } from '@/lib/contentPacks/httpContentPackApi';

const defaultGetProgress = createDefaultGetProgressUseCase();
const defaultListPacks = createHttpContentPackApi().listPacks;
const defaultDownloadPack = createDefaultContentPackDownloader();
const defaultStorage = createDefaultStorageManager();

function CompletionBar({ percentage, tint }: { percentage: number; tint: string }) {
  const width = Math.max(0, Math.min(100, percentage));
  return (
    <View
      style={styles.track}
      accessibilityRole="progressbar"
      accessibilityValue={{ min: 0, max: 100, now: width }}>
      <View style={[styles.fill, { width: `${width}%`, backgroundColor: tint }]} />
    </View>
  );
}

export default function ProfileScreen({
  getProgress = defaultGetProgress,
  listPacks = defaultListPacks,
  downloadPack = defaultDownloadPack,
  getStorageSummary = defaultStorage.getSummary,
  deletePack = defaultStorage.deletePack,
}: {
  getProgress?: (input: { languageId: string; userId: string }) => Promise<GetProgressResult>;
  listPacks?: (languageId: string) => Promise<ContentPackListItem[]>;
  downloadPack?: (input: {
    languageId: string;
    packId: string;
    onProgress?: (progress: PackDownloadProgress) => void;
  }) => Promise<DownloadContentPackResult>;
  getStorageSummary?: (languageId: string) => Promise<StorageSummary>;
  deletePack?: (languageId: string, packId: string) => Promise<void>;
}) {
  const { user, signOut } = useAuth();
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const { speakingMinutes, listeningMinutes } = useSpeakingListeningStats(user?.id ?? null);
  const [progress, setProgress] = useState<GetProgressResult | null>(null);
  const [packs, setPacks] = useState<ContentPackListItem[]>([]);
  const [storage, setStorage] = useState<StorageSummary | null>(null);
  const [deletingPackId, setDeletingPackId] = useState<string | null>(null);
  const [loading, setLoading] = useState(Boolean(user));

  const load = useCallback(async () => {
    if (!user) {
      setProgress(null);
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      setProgress(await getProgress({ languageId: getLanguageId(), userId: user.id }));
    } catch (caught) {
      const message = caught instanceof Error ? caught.message : 'Could not load progress.';
      toast.error(message);
      setProgress(null);
    }

    try {
      setPacks(await listPacks(getLanguageId()));
    } catch {
      setPacks([]);
    }

    try {
      setStorage(await getStorageSummary(getLanguageId()));
    } catch {
      setStorage({ totalBytes: 0, packs: [] });
    } finally {
      setLoading(false);
    }
  }, [getProgress, getStorageSummary, listPacks, user]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  if (!user) {
    return (
      <SafeAreaView style={styles.safe} edges={['top']}>
        <ThemedView style={styles.padded}>
          <ThemedText type="title">Profile</ThemedText>
          <ThemedText>Sign in to see XP, streaks, and lesson progress.</ThemedText>
        </ThemedView>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <ScrollView contentContainerStyle={styles.padded}>
        <ThemedText type="title">Profile</ThemedText>
        <ThemedText type="subtitle">{user.displayName}</ThemedText>
        <ThemedText style={{ color: colors.icon }}>{user.email}</ThemedText>

        {loading && !progress ? <ActivityIndicator /> : null}

        {progress ? (
          <View style={styles.section}>
            <ThemedText type="defaultSemiBold" accessibilityLabel={`${progress.totalXp} XP`}>
              {progress.totalXp} XP
            </ThemedText>
            <ThemedText accessibilityLabel={`Streak ${progress.currentStreak} days`}>
              {progress.currentStreak} day streak · longest {progress.longestStreak}
            </ThemedText>
            <ThemedText>Level: {progress.progressLevel}</ThemedText>
            <ThemedText accessibilityLabel={`${speakingMinutes} minutes speaking`}>
              {speakingMinutes} min speaking
            </ThemedText>
            <ThemedText accessibilityLabel={`${listeningMinutes} minutes listening`}>
              {listeningMinutes} min listening
            </ThemedText>
          </View>
        ) : null}

        {progress && progress.byLevel.length > 0 ? (
          <View style={styles.section}>
            <ThemedText type="subtitle">By level</ThemedText>
            {progress.byLevel.map((bucket) => (
              <View key={bucket.level ?? 'level'} style={styles.bucket}>
                <ThemedText>
                  {bucket.level} · {bucket.completedCount}/{bucket.totalCount} ({bucket.percentage}%)
                </ThemedText>
                <CompletionBar percentage={bucket.percentage} tint={colors.tint} />
              </View>
            ))}
          </View>
        ) : null}

        {progress && progress.byCategory.length > 0 ? (
          <View style={styles.section}>
            <ThemedText type="subtitle">By category</ThemedText>
            {progress.byCategory.map((bucket) => (
              <View key={bucket.category ?? 'category'} style={styles.bucket}>
                <ThemedText>
                  {bucket.category} · {bucket.completedCount}/{bucket.totalCount} ({bucket.percentage}%)
                </ThemedText>
                <CompletionBar percentage={bucket.percentage} tint={colors.tint} />
              </View>
            ))}
          </View>
        ) : null}

        {progress ? (
          <View style={styles.section}>
            <ThemedText type="subtitle">Weak areas</ThemedText>
            {progress.weakAreas.length === 0 ? (
              <ThemedText>No weak areas yet.</ThemedText>
            ) : (
              progress.weakAreas.map((area) => (
                <ThemedText key={`${area.level}-${area.category}`}>
                  {area.level} · {area.category} — avg {area.averageScore} ({area.sampleSize}{' '}
                  {area.sampleSize === 1 ? 'score' : 'scores'})
                </ThemedText>
              ))
            )}
          </View>
        ) : null}

        <ContentPackDownloadPanel
          languageId={getLanguageId()}
          packs={packs}
          downloadPack={downloadPack}
        />

        {storage ? (
          <StorageManagerPanel
            summary={storage}
            deletingPackId={deletingPackId}
            onDeletePack={(packId) => {
              setDeletingPackId(packId);
              void deletePack(getLanguageId(), packId)
                .then(() => load())
                .catch((error) => {
                  const message =
                    error instanceof ContentPackError ? error.message : 'Could not delete that pack.';
                  toast.error(message);
                })
                .finally(() => setDeletingPackId(null));
            }}
          />
        ) : null}

        <Pressable onPress={() => void signOut()} accessibilityRole="button" accessibilityLabel="Sign out">
          <ThemedText type="link">Sign out</ThemedText>
        </Pressable>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
  },
  padded: {
    padding: 24,
    gap: 16,
  },
  section: {
    gap: 8,
  },
  bucket: {
    gap: 6,
  },
  track: {
    height: 8,
    borderRadius: 4,
    backgroundColor: '#d0d5d8',
    overflow: 'hidden',
  },
  fill: {
    height: 8,
    borderRadius: 4,
  },
});
