import { fireEvent, render, waitFor } from '@testing-library/react-native';

import { AudioPrompt } from '@/components/lesson/AudioPrompt';
import type { AudioPlayer } from '@/domain/audio/audioPlayer';
import { PlaybackRate } from '@/domain/audio/types';
import type { AudioRef } from '@/domain/catalog/types';

function recording(id: string, dialectLabel: string | null = null): AudioRef {
  return {
    id,
    cdnUrl: `https://cdn.example/${id}.mp3`,
    fileFormat: 'Mp3',
    speakerGender: 'Female',
    dialectLabel,
    durationMs: 400,
  };
}

function createFakePlayer(): AudioPlayer {
  let selectedId: string | null = 'rec-1';
  let rate: PlaybackRate = PlaybackRate.Normal;

  return {
    recordings: () => [],
    selectedRecordingId: () => selectedId,
    playbackRate: () => rate,
    lastSource: () => null,
    selectRecording: jest.fn((id) => {
      selectedId = id;
    }),
    setRate: jest.fn(async (next) => {
      rate = next;
    }),
    play: jest.fn(async () => ({ kind: 'cdn' as const, uri: 'https://cdn.example/rec-1.mp3', recordingId: 'rec-1' })),
    replay: jest.fn(async () => ({ kind: 'cdn' as const, uri: 'https://cdn.example/rec-1.mp3', recordingId: 'rec-1' })),
    stop: async () => undefined,
    dispose: async () => undefined,
  };
}

describe('AudioPrompt', () => {
  it('plays, toggles 0.75x, and selects among multiple recordings', async () => {
    const player = createFakePlayer();
    const createPlayer = jest.fn(() => player);
    const { getByLabelText } = render(
      <AudioPrompt
        languageId="lang-1"
        recordings={[recording('rec-1', 'Western'), recording('rec-2', 'Eastern')]}
        ttsText="Mhoro"
        createPlayer={createPlayer}
      />,
    );

    expect(createPlayer).toHaveBeenCalledWith(
      expect.objectContaining({ languageId: 'lang-1', ttsText: 'Mhoro' }),
    );

    fireEvent.press(getByLabelText('Play audio'));
    await waitFor(() => expect(player.play).toHaveBeenCalled());

    fireEvent.press(getByLabelText('Playback speed 1x'));
    await waitFor(() => expect(player.setRate).toHaveBeenCalledWith(PlaybackRate.Slow));

    fireEvent.press(getByLabelText('Eastern · Female'));
    expect(player.selectedRecordingId()).toBe('rec-2');
  });
});
