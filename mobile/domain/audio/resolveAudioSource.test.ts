import { AudioUnavailableError } from '@/domain/audio/errors';
import { resolveAudioSource } from '@/domain/audio/resolveAudioSource';
import { AudioSourceKind } from '@/domain/audio/types';
import type { AudioRef } from '@/domain/catalog/types';

const languageId = 'lang-1';

const recording: AudioRef = {
  id: 'rec-1',
  cdnUrl: 'https://cdn.example/rec-1.mp3',
  fileFormat: 'Mp3',
  speakerGender: 'Unspecified',
  dialectLabel: null,
  durationMs: 800,
};

describe('resolveAudioSource', () => {
  it('prefers a local offline file over the CDN URL', async () => {
    const source = await resolveAudioSource(
      { languageId, recordings: [recording] },
      {
        documentDirectory: () => 'file:///docs/',
        localAudioExists: { exists: async () => true },
      },
    );

    expect(source).toEqual({
      kind: AudioSourceKind.Local,
      uri: 'file:///docs/offline/audio/lang-1/rec-1.mp3',
      recordingId: 'rec-1',
    });
  });

  it('falls back to the CDN when the local file is missing', async () => {
    const source = await resolveAudioSource(
      { languageId, recordings: [recording] },
      {
        documentDirectory: () => 'file:///docs/',
        localAudioExists: { exists: async () => false },
      },
    );

    expect(source).toEqual({
      kind: AudioSourceKind.Cdn,
      uri: recording.cdnUrl,
      recordingId: 'rec-1',
    });
  });

  it('uses TTS when no recording URI is playable', async () => {
    const source = await resolveAudioSource(
      {
        languageId,
        recordings: [{ ...recording, cdnUrl: '  ' }],
        ttsText: 'Mhoro',
      },
      {
        documentDirectory: () => 'file:///docs/',
        localAudioExists: { exists: async () => false },
      },
    );

    expect(source).toEqual({ kind: AudioSourceKind.Tts, text: 'Mhoro' });
  });

  it('throws when nothing can be played', async () => {
    await expect(
      resolveAudioSource(
        { languageId, recordings: [] },
        {
          documentDirectory: () => 'file:///docs/',
          localAudioExists: { exists: async () => false },
        },
      ),
    ).rejects.toBeInstanceOf(AudioUnavailableError);
  });
});
