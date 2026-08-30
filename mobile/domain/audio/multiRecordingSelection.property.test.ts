import * as fc from 'fast-check';

import { createAudioPlayer } from '@/domain/audio/audioPlayer';
import { AudioRecordingNotFoundError } from '@/domain/audio/errors';
import { AudioSourceKind, type AudioPlaybackEngine } from '@/domain/audio/types';
import type { AudioRef } from '@/domain/catalog/types';

const languageId = '11111111-1111-7111-8111-111111111111';

const recordingArb: fc.Arbitrary<AudioRef> = fc.record({
  id: fc.uuid(),
  cdnUrl: fc.uuid().map((id) => `https://cdn.example/${id}.mp3`),
  fileFormat: fc.constantFrom('Mp3', 'Aac'),
  speakerGender: fc.constantFrom('Unspecified', 'Male', 'Female'),
  dialectLabel: fc.option(fc.constantFrom('Western', 'Eastern'), { nil: null }),
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

describe('multi-recording phrase audio selection', () => {
  // Feature: kalanga-language-app, Property 9: Multi-Recording Phrase Audio Selection
  it('exposes every phrase recording and plays each one independently', async () => {
    await fc.assert(
      fc.asyncProperty(
        fc.uniqueArray(recordingArb, { minLength: 2, maxLength: 5, selector: (item) => item.id }),
        fc.uuid(),
        async (recordings, outsiderId) => {
          const playback = createPlayback();
          const player = createAudioPlayer({
            documentDirectory: () => 'file:///docs/',
            localAudioExists: { exists: async () => false },
            playback,
            tts: { speak: async () => undefined, stop: async () => undefined },
          })({ languageId, recordings });

          expect(player.recordings()).toEqual(recordings);
          expect(player.recordings().map((item) => item.id)).toEqual(recordings.map((item) => item.id));

          for (const recording of recordings) {
            player.selectRecording(recording.id);
            expect(player.selectedRecordingId()).toBe(recording.id);

            const source = await player.play();
            expect(source.kind).toBe(AudioSourceKind.Cdn);
            expect(source).toMatchObject({ recordingId: recording.id, uri: recording.cdnUrl });
          }

          expect(playback.loaded).toEqual(recordings.map((recording) => recording.cdnUrl));

          if (!recordings.some((recording) => recording.id === outsiderId)) {
            expect(() => player.selectRecording(outsiderId)).toThrow(AudioRecordingNotFoundError);
          }
        },
      ),
      { numRuns: 100 },
    );
  });
});
