import { PLAYBACK_RATE_NORMAL } from '@/constants/audio';
import { AudioPlaybackError, AudioRecordingNotFoundError } from '@/domain/audio/errors';
import { resolveAudioSource } from '@/domain/audio/resolveAudioSource';
import {
  AudioSourceKind,
  PlaybackRate,
  type AudioPlaybackSession,
  type AudioPlayerDeps,
  type ResolvedAudioSource,
} from '@/domain/audio/types';
import type { AudioRef } from '@/domain/catalog/types';
import type { EntityId } from '@/domain/entities';

export type AudioPlayer = {
  recordings(): readonly AudioRef[];
  selectedRecordingId(): EntityId | null;
  playbackRate(): PlaybackRate;
  lastSource(): ResolvedAudioSource | null;
  selectRecording(recordingId: EntityId): void;
  setRate(rate: PlaybackRate): Promise<void>;
  play(): Promise<ResolvedAudioSource>;
  replay(): Promise<ResolvedAudioSource>;
  stop(): Promise<void>;
  dispose(): Promise<void>;
};

export function createAudioPlayer(deps: AudioPlayerDeps) {
  return function attachAudioPlayer(session: AudioPlaybackSession): AudioPlayer {
    const recordings = session.recordings;
    let selectedId: EntityId | null = recordings[0]?.id ?? null;
    let rate: PlaybackRate = PLAYBACK_RATE_NORMAL;
    let lastSource: ResolvedAudioSource | null = null;
    let loadedUri: string | null = null;

    function requireSelectedRecording(recordingId: EntityId): AudioRef {
      const match = recordings.find((item) => item.id === recordingId);
      if (!match) {
        throw new AudioRecordingNotFoundError(recordingId);
      }

      return match;
    }

    async function resolveCurrent(): Promise<ResolvedAudioSource> {
      return resolveAudioSource(
        {
          languageId: session.languageId,
          recordings,
          recordingId: selectedId,
          ttsText: session.ttsText,
        },
        { localAudioExists: deps.localAudioExists, documentDirectory: deps.documentDirectory },
      );
    }

    async function playUri(uri: string, kind: typeof AudioSourceKind.Local | typeof AudioSourceKind.Cdn): Promise<void> {
      await deps.tts.stop();
      if (loadedUri !== uri) {
        await deps.playback.unload();
        try {
          await deps.playback.load(uri, rate);
          loadedUri = uri;
        } catch (error) {
          loadedUri = null;
          if (kind === AudioSourceKind.Cdn && session.ttsText?.trim()) {
            const text = session.ttsText.trim();
            lastSource = { kind: AudioSourceKind.Tts, text };
            await deps.tts.speak(text, rate);
            return;
          }

          throw error instanceof AudioPlaybackError
            ? error
            : new AudioPlaybackError('Audio playback failed.');
        }
      } else {
        await deps.playback.setRate(rate);
      }

      await deps.playback.play();
    }

    async function speak(text: string): Promise<void> {
      await deps.playback.stop();
      await deps.tts.speak(text, rate);
    }

    return {
      recordings: () => recordings,
      selectedRecordingId: () => selectedId,
      playbackRate: () => rate,
      lastSource: () => lastSource,
      selectRecording(recordingId: EntityId) {
        requireSelectedRecording(recordingId);
        if (selectedId !== recordingId) {
          selectedId = recordingId;
          loadedUri = null;
        }
      },
      async setRate(nextRate: PlaybackRate) {
        if (nextRate !== PlaybackRate.Normal && nextRate !== PlaybackRate.Slow) {
          throw new AudioPlaybackError('Unsupported playback rate.');
        }

        rate = nextRate;
        if (loadedUri) {
          await deps.playback.setRate(rate);
        }
      },
      async play() {
        const source = await resolveCurrent();
        lastSource = source;
        if (source.kind === AudioSourceKind.Tts) {
          loadedUri = null;
          await speak(source.text);
          return source;
        }

        await playUri(source.uri, source.kind);
        return lastSource ?? source;
      },
      async replay() {
        if (lastSource?.kind === AudioSourceKind.Tts) {
          await speak(lastSource.text);
          return lastSource;
        }

        if (loadedUri) {
          await deps.tts.stop();
          await deps.playback.setRate(rate);
          await deps.playback.replay();
          return lastSource ?? (await resolveCurrent());
        }

        return this.play();
      },
      async stop() {
        await deps.playback.stop();
        await deps.tts.stop();
      },
      async dispose() {
        loadedUri = null;
        lastSource = null;
        await deps.playback.unload();
        await deps.tts.stop();
      },
    };
  };
}
