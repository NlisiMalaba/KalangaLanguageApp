import * as fc from 'fast-check';

import { createAudioPlayer } from '@/domain/audio/audioPlayer';
import { localAudioFileUri } from '@/domain/audio/localAudioPath';
import { AudioSourceKind, type AudioPlaybackEngine, type TtsEngine } from '@/domain/audio/types';
import type { AudioRef } from '@/domain/catalog/types';

const languageId = '11111111-1111-7111-8111-111111111111';
const documentDirectory = 'file:///docs/';

const recordingArb: fc.Arbitrary<AudioRef> = fc.record({
  id: fc.uuid(),
  cdnUrl: fc.uuid().map((id) => `https://cdn.example/${id}.mp3`),
  fileFormat: fc.constantFrom('Mp3', 'Aac'),
  speakerGender: fc.constant('Unspecified'),
  dialectLabel: fc.constant(null),
  durationMs: fc.integer({ min: 200, max: 8_000 }),
});

function createPlayback(): AudioPlaybackEngine & { loaded: string[] } {
  const loaded: string[] = [];
  return {
    loaded,
    load: async (uri) => {
      loaded.push(uri);
    },
    play: async () => undefined,
    replay: async () => undefined,
    setRate: async () => undefined,
    stop: async () => undefined,
    unload: async () => undefined,
  };
}

function createTts(): TtsEngine & { spoken: string[] } {
  const spoken: string[] = [];
  return {
    spoken,
    speak: async (text) => {
      spoken.push(text);
    },
    stop: async () => undefined,
  };
}

describe('offline audio resolves to local file', () => {
  // Feature: kalanga-language-app, Property 10: Offline Audio Resolves to Local File
  it('plays the downloaded local file and never requests the CDN', async () => {
    await fc.assert(
      fc.asyncProperty(
        fc.uniqueArray(recordingArb, { minLength: 1, maxLength: 4, selector: (item) => item.id }),
        async (recordings) => {
          const playback = createPlayback();
          const tts = createTts();
          const expectedLocal = new Map(
            recordings.map((recording) => [
              recording.id,
              localAudioFileUri(documentDirectory, languageId, recording.id, recording.fileFormat),
            ]),
          );
          const localFiles = new Set(expectedLocal.values());

          const player = createAudioPlayer({
            documentDirectory: () => documentDirectory,
            localAudioExists: {
              exists: async (uri) => localFiles.has(uri),
            },
            playback,
            tts,
          })({ languageId, recordings, ttsText: 'unused' });

          for (const recording of recordings) {
            player.selectRecording(recording.id);
            const source = await player.play();
            const localUri = expectedLocal.get(recording.id);

            expect(source.kind).toBe(AudioSourceKind.Local);
            expect(source).toMatchObject({ uri: localUri, recordingId: recording.id });
            expect(localUri).not.toBe(recording.cdnUrl);
          }

          expect(playback.loaded).toEqual(recordings.map((recording) => expectedLocal.get(recording.id)));
          expect(playback.loaded.some((uri) => uri.startsWith('https://'))).toBe(false);
          expect(tts.spoken).toEqual([]);
        },
      ),
      { numRuns: 100 },
    );
  });
});
