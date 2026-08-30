import { fireEvent, render, waitFor } from '@testing-library/react-native';

import { PronunciationPractice } from '@/components/pronunciation/PronunciationPractice';
import type { AudioPlayer } from '@/domain/audio/audioPlayer';
import { PlaybackRate } from '@/domain/audio/types';
import type { AudioRef, LessonPhraseDetail } from '@/domain/catalog/types';
import { MicrophonePermissionDeniedError } from '@/domain/pronunciation/errors';
import type { PronunciationRecorder } from '@/domain/pronunciation/pronunciationRecorder';

const phrase: LessonPhraseDetail = {
  id: 'phrase-1',
  kalangaText: 'Mhoro',
  englishTranslation: 'Hello',
  sortOrder: 0,
  variations: [],
  audio: [
    {
      id: 'rec-1',
      cdnUrl: 'https://cdn.example/a.mp3',
      fileFormat: 'Mp3',
      speakerGender: 'Female',
      dialectLabel: null,
      durationMs: 900,
    },
  ],
};

const recordings: AudioRef[] = phrase.audio;

function createFakePlayer(): AudioPlayer {
  return {
    recordings: () => recordings,
    selectedRecordingId: () => 'rec-1',
    playbackRate: () => PlaybackRate.Normal,
    lastSource: () => null,
    selectRecording: jest.fn(),
    setRate: jest.fn(async () => undefined),
    play: jest.fn(async () => ({ kind: 'cdn' as const, uri: 'https://cdn.example/a.mp3', recordingId: 'rec-1' })),
    replay: jest.fn(async () => ({ kind: 'cdn' as const, uri: 'https://cdn.example/a.mp3', recordingId: 'rec-1' })),
    stop: async () => undefined,
    dispose: async () => undefined,
  };
}

function createFakeRecorder(overrides: Partial<PronunciationRecorder> = {}): PronunciationRecorder {
  return {
    ensurePermission: jest.fn(async () => undefined),
    openSettings: jest.fn(async () => undefined),
    start: jest.fn(async () => undefined),
    stop: jest.fn(async () => ({
      uri: 'file:///docs/offline/pronunciation/lang/a.m4a',
      durationMs: 900,
      energyProfile: [],
    })),
    ...overrides,
  };
}

describe('PronunciationPractice', () => {
  it('records locally, scores, and never uploads without consent', async () => {
    const recorder = createFakeRecorder();
    const player = createFakePlayer();
    const upload = jest.fn(async () => undefined);
    const { getByLabelText, getByText } = render(
      <PronunciationPractice
        languageId="lang-1"
        phrase={phrase}
        recordings={recordings}
        createRecorder={() => recorder}
        createPlayer={() => player}
        consentGranted={false}
        transmitter={{ upload }}
      />,
    );

    fireEvent.press(getByLabelText('Play audio'));
    await waitFor(() => expect(player.play).toHaveBeenCalled());

    fireEvent.press(getByLabelText('Record pronunciation'));
    await waitFor(() => expect(getByLabelText('Stop recording')).toBeTruthy());
    expect(recorder.start).toHaveBeenCalledWith('lang-1');
    expect(getByLabelText('Recording in progress')).toBeTruthy();

    fireEvent.press(getByLabelText('Stop recording'));
    await waitFor(() => expect(recorder.stop).toHaveBeenCalled());
    await waitFor(() => expect(getByText('100')).toBeTruthy());
    expect(getByLabelText('Feedback Excellent')).toBeTruthy();
    expect(upload).not.toHaveBeenCalled();
  });

  it('opens the permission modal when the microphone is denied', async () => {
    const recorder = createFakeRecorder({
      start: jest.fn(async () => {
        throw new MicrophonePermissionDeniedError();
      }),
    });
    const { getByLabelText } = render(
      <PronunciationPractice
        languageId="lang-1"
        phrase={phrase}
        recordings={recordings}
        createRecorder={() => recorder}
        createPlayer={() => createFakePlayer()}
      />,
    );

    fireEvent.press(getByLabelText('Record pronunciation'));
    await waitFor(() => expect(getByLabelText('Open settings')).toBeTruthy());
  });
});
