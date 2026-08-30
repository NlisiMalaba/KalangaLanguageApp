import AsyncStorage from '@react-native-async-storage/async-storage';

import { addListeningMs, addSpeakingMs, loadSpeakingListeningStats, minutesFromMs } from '@/lib/speakingListeningStats';

describe('speakingListeningStats', () => {
  beforeEach(async () => {
    await AsyncStorage.clear();
  });

  it('accumulates speaking and listening time per learner', async () => {
    await addSpeakingMs('user-1', 90_000);
    await addListeningMs('user-1', 30_000);
    await addListeningMs('user-2', 60_000);

    const first = await loadSpeakingListeningStats('user-1');
    expect(first.speakingMs).toBe(90_000);
    expect(first.listeningMs).toBe(30_000);
    expect(minutesFromMs(first.speakingMs)).toBe(1.5);
    expect((await loadSpeakingListeningStats('user-2')).listeningMs).toBe(60_000);
  });
});
