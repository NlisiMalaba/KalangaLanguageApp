import { fireEvent, render, waitFor } from '@testing-library/react-native';

import { ListeningMultipleChoiceMode } from '@/components/lesson/ListeningMultipleChoiceMode';
import type { AudioPlayer } from '@/domain/audio/audioPlayer';
import { PlaybackRate } from '@/domain/audio/types';
import type { AudioRef } from '@/domain/catalog/types';

const recordings: AudioRef[] = [
  {
    id: 'rec-1',
    cdnUrl: 'https://cdn.example/a.mp3',
    fileFormat: 'Mp3',
    speakerGender: 'Female',
    dialectLabel: null,
    durationMs: 400,
  },
];

function createFakePlayer(): AudioPlayer {
  return {
    recordings: () => recordings,
    selectedRecordingId: () => 'rec-1',
    playbackRate: () => PlaybackRate.Normal,
    lastSource: () => null,
    selectRecording: jest.fn(),
    setRate: jest.fn(async () => undefined),
    play: jest.fn(async () => ({ kind: 'cdn' as const, uri: recordings[0]!.cdnUrl, recordingId: 'rec-1' })),
    replay: jest.fn(async () => ({ kind: 'cdn' as const, uri: recordings[0]!.cdnUrl, recordingId: 'rec-1' })),
    stop: async () => undefined,
    dispose: async () => undefined,
  };
}

describe('ListeningMultipleChoiceMode', () => {
  it('plays reference audio and accepts a four-option answer', async () => {
    const player = createFakePlayer();
    const onSelect = jest.fn();
    const { getByLabelText, getByText } = render(
      <ListeningMultipleChoiceMode
        languageId="lang-1"
        recordings={recordings}
        ttsText="Mhoro"
        options={['Hello', 'Goodbye', 'Please', 'Thanks']}
        selectedIndex={null}
        onSelect={onSelect}
        createPlayer={() => player}
      />,
    );

    expect(getByText('What did you hear?')).toBeTruthy();
    fireEvent.press(getByLabelText('Play audio'));
    await waitFor(() => expect(player.play).toHaveBeenCalled());
    fireEvent.press(getByLabelText('Option 3: Please'));
    expect(onSelect).toHaveBeenCalledWith(2);
  });
});
