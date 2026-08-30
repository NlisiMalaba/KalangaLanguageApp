import AsyncStorage from '@react-native-async-storage/async-storage';

import { MS_PER_MINUTE } from '@/constants/progress';
import type { EntityId } from '@/domain/entities';

export type SpeakingListeningStats = {
  speakingMs: number;
  listeningMs: number;
};

const emptyStats: SpeakingListeningStats = {
  speakingMs: 0,
  listeningMs: 0,
};

function storageKey(userId: EntityId): string {
  return `kalanga.practiceMinutes.${userId}`;
}

export function minutesFromMs(ms: number): number {
  if (ms <= 0) {
    return 0;
  }

  return Math.round((ms / MS_PER_MINUTE) * 10) / 10;
}

export async function loadSpeakingListeningStats(userId: EntityId): Promise<SpeakingListeningStats> {
  const raw = await AsyncStorage.getItem(storageKey(userId));
  if (!raw) {
    return emptyStats;
  }

  try {
    const parsed = JSON.parse(raw) as Partial<SpeakingListeningStats>;
    return {
      speakingMs: Math.max(0, Number(parsed.speakingMs) || 0),
      listeningMs: Math.max(0, Number(parsed.listeningMs) || 0),
    };
  } catch {
    return emptyStats;
  }
}

async function save(userId: EntityId, stats: SpeakingListeningStats): Promise<SpeakingListeningStats> {
  await AsyncStorage.setItem(storageKey(userId), JSON.stringify(stats));
  return stats;
}

export async function addSpeakingMs(userId: EntityId, durationMs: number): Promise<SpeakingListeningStats> {
  const current = await loadSpeakingListeningStats(userId);
  if (durationMs <= 0) {
    return current;
  }

  return save(userId, { ...current, speakingMs: current.speakingMs + durationMs });
}

export async function addListeningMs(userId: EntityId, durationMs: number): Promise<SpeakingListeningStats> {
  const current = await loadSpeakingListeningStats(userId);
  if (durationMs <= 0) {
    return current;
  }

  return save(userId, { ...current, listeningMs: current.listeningMs + durationMs });
}
