import type { AudioPlayer } from '@/domain/audio/audioPlayer';

let active: AudioPlayer | null = null;

export async function stopOtherPhrasePlayers(next: AudioPlayer): Promise<void> {
  if (active && active !== next) {
    await active.stop();
  }

  active = next;
}

export function clearActivePhrasePlayer(player: AudioPlayer): void {
  if (active === player) {
    active = null;
  }
}
