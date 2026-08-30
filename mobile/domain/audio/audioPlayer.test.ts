import { createAudioPlayer } from '@/domain/audio/audioPlayer';
import { AudioRecordingNotFoundError } from '@/domain/audio/errors';
import { AudioSourceKind, PlaybackRate, type AudioPlaybackEngine, type TtsEngine } from '@/domain/audio/types';
import type { AudioRef } from '@/domain/catalog/types';

const languageId = 'lang-1';

function recording(id: string, cdnUrl = `https://cdn.example/${id}.mp3`): AudioRef {
  return {
    id,
    cdnUrl,
    fileFormat: 'Mp3',
    speakerGender: 'Unspecified',
    dialectLabel: id === 'rec-2' ? 'Western' : null,
    durationMs: 800,
  };
}

function createPlayback(): AudioPlaybackEngine & { calls: string[] } {
  const calls: string[] = [];
  return {
    calls,
    load: async (uri) => {
      calls.push(`load:${uri}`);
    },
    play: async () => {
      calls.push('play');
    },
    replay: async () => {
      calls.push('replay');
    },
    setRate: async (rate) => {
      calls.push(`rate:${rate}`);
    },
    stop: async () => {
      calls.push('stop');
    },
    unload: async () => {
      calls.push('unload');
    },
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

describe('AudioPlayer', () => {
  it('exposes every recording and plays the selected one', async () => {
    const recordings = [recording('rec-1'), recording('rec-2')];
    const playback = createPlayback();
    const player = createAudioPlayer({
      documentDirectory: () => 'file:///docs/',
      localAudioExists: { exists: async () => false },
      playback,
      tts: createTts(),
    })({ languageId, recordings });

    expect(player.recordings().map((item) => item.id)).toEqual(['rec-1', 'rec-2']);
    expect(player.selectedRecordingId()).toBe('rec-1');

    const first = await player.play();
    expect(first.kind).toBe(AudioSourceKind.Cdn);
    expect(first).toMatchObject({ recordingId: 'rec-1' });

    player.selectRecording('rec-2');
    const second = await player.play();
    expect(second).toMatchObject({ kind: AudioSourceKind.Cdn, recordingId: 'rec-2' });
    expect(playback.calls.filter((call) => call.startsWith('load:'))).toEqual([
      'load:https://cdn.example/rec-1.mp3',
      'load:https://cdn.example/rec-2.mp3',
    ]);
  });

  it('replays the loaded clip without a new load', async () => {
    const playback = createPlayback();
    const player = createAudioPlayer({
      documentDirectory: () => 'file:///docs/',
      localAudioExists: { exists: async () => true },
      playback,
      tts: createTts(),
    })({ languageId, recordings: [recording('rec-1')] });

    await player.play();
    await player.replay();

    expect(playback.calls.filter((call) => call === 'replay')).toEqual(['replay']);
    expect(playback.calls.filter((call) => call === 'play')).toEqual(['play']);
    expect(playback.calls.filter((call) => call.startsWith('load:'))).toHaveLength(1);
  });

  it('applies 0.75x playback rate on the current sound', async () => {
    const playback = createPlayback();
    const player = createAudioPlayer({
      documentDirectory: () => 'file:///docs/',
      localAudioExists: { exists: async () => false },
      playback,
      tts: createTts(),
    })({ languageId, recordings: [recording('rec-1')] });

    await player.play();
    await player.setRate(PlaybackRate.Slow);
    expect(player.playbackRate()).toBe(0.75);
    expect(playback.calls).toContain('rate:0.75');
  });

  it('rejects a recording that is not on the phrase', () => {
    const player = createAudioPlayer({
      documentDirectory: () => 'file:///docs/',
      localAudioExists: { exists: async () => false },
      playback: createPlayback(),
      tts: createTts(),
    })({ languageId, recordings: [recording('rec-1')] });

    expect(() => player.selectRecording('missing')).toThrow(AudioRecordingNotFoundError);
  });

  it('falls back to TTS when CDN load fails', async () => {
    const tts = createTts();
    const player = createAudioPlayer({
      documentDirectory: () => 'file:///docs/',
      localAudioExists: { exists: async () => false },
      playback: {
        ...createPlayback(),
        load: async () => {
          throw new Error('cdn down');
        },
      },
      tts,
    })({ languageId, recordings: [recording('rec-1')], ttsText: 'Mhoro' });

    const source = await player.play();
    expect(source.kind).toBe(AudioSourceKind.Tts);
    expect(tts.spoken).toEqual(['Mhoro']);
  });
});
