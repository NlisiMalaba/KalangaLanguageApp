import { useCallback, useState } from 'react';
import { useFocusEffect } from 'expo-router';

import type { EntityId } from '@/domain/entities';
import {
  addListeningMs,
  addSpeakingMs,
  loadSpeakingListeningStats,
  minutesFromMs,
  type SpeakingListeningStats,
} from '@/lib/speakingListeningStats';

const empty: SpeakingListeningStats = { speakingMs: 0, listeningMs: 0 };

export function useSpeakingListeningStats(userId: EntityId | null) {
  const [stats, setStats] = useState<SpeakingListeningStats>(empty);

  const reload = useCallback(async () => {
    if (!userId) {
      setStats(empty);
      return;
    }

    setStats(await loadSpeakingListeningStats(userId));
  }, [userId]);

  useFocusEffect(
    useCallback(() => {
      void reload();
    }, [reload]),
  );

  const recordSpeaking = useCallback(
    async (durationMs: number) => {
      if (!userId) {
        return;
      }

      setStats(await addSpeakingMs(userId, durationMs));
    },
    [userId],
  );

  const recordListening = useCallback(
    async (durationMs: number) => {
      if (!userId) {
        return;
      }

      setStats(await addListeningMs(userId, durationMs));
    },
    [userId],
  );

  return {
    speakingMinutes: minutesFromMs(stats.speakingMs),
    listeningMinutes: minutesFromMs(stats.listeningMs),
    reload,
    recordSpeaking,
    recordListening,
  };
}
