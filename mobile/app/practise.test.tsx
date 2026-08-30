import { fireEvent, render, waitFor } from '@testing-library/react-native';

import PractiseScreen from '@/app/practise';
import type { LessonDetail } from '@/domain/catalog/types';
import { Level } from '@/domain/enums';
import type { PronunciationRecorder } from '@/domain/pronunciation/pronunciationRecorder';

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ lessonId: 'lesson-1', phraseId: 'phrase-1' }),
  useFocusEffect: (effect: () => void | (() => void)) => {
    const { useEffect } = require('react') as typeof import('react');
    useEffect(() => {
      return effect();
    }, [effect]);
  },
}));

const lesson: LessonDetail = {
  id: 'lesson-1',
  languageId: 'lang-1',
  title: 'Greetings',
  level: Level.Beginner,
  category: 'Everyday',
  isScenario: false,
  scenarioContext: null,
  xpReward: 10,
  updatedAt: '2026-08-23T12:00:00.000Z',
  phrases: [
    {
      id: 'phrase-1',
      kalangaText: 'Mhoro',
      englishTranslation: 'Hello',
      sortOrder: 0,
      variations: [],
      audio: [
        {
          id: 'audio-1',
          cdnUrl: 'https://cdn.example/a.mp3',
          fileFormat: 'Mp3',
          speakerGender: 'Female',
          dialectLabel: null,
          durationMs: 900,
        },
      ],
    },
    {
      id: 'phrase-2',
      kalangaText: 'Ndatenda',
      englishTranslation: 'Thank you',
      sortOrder: 1,
      variations: [],
      audio: [],
    },
  ],
  exercises: [],
};

describe('PractiseScreen', () => {
  it('loads a lesson phrase and scores an on-device recording', async () => {
    const recorder: PronunciationRecorder = {
      ensurePermission: jest.fn(async () => undefined),
      openSettings: jest.fn(async () => undefined),
      start: jest.fn(async () => undefined),
      stop: jest.fn(async () => ({
        uri: 'file:///docs/offline/pronunciation/lang/a.m4a',
        durationMs: 900,
        energyProfile: [],
      })),
    };
    const getLesson = jest.fn(async () => lesson);

    const { getByText, getByLabelText } = render(
      <PractiseScreen getLesson={getLesson} createRecorder={() => recorder} />,
    );

    await waitFor(() => expect(getByText('Mhoro')).toBeTruthy());
    expect(getByText('Greetings · 1 of 2')).toBeTruthy();
    expect(getByLabelText('Play audio')).toBeTruthy();

    fireEvent.press(getByLabelText('Record pronunciation'));
    await waitFor(() => expect(getByLabelText('Stop recording')).toBeTruthy());
    expect(recorder.start).toHaveBeenCalledWith('lang-1');
    fireEvent.press(getByLabelText('Stop recording'));
    await waitFor(() => expect(getByLabelText('Feedback Excellent')).toBeTruthy());
  });
});
