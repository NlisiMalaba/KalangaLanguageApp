import { fireEvent, render } from '@testing-library/react-native';

import { LessonContent } from '@/components/lesson/LessonContent';
import type { LessonDetail } from '@/domain/catalog/types';
import { Level } from '@/domain/enums';

const lesson: LessonDetail = {
  id: 'lesson-1',
  languageId: 'lang-1',
  title: 'At the market',
  level: Level.Beginner,
  category: 'Everyday',
  isScenario: true,
  scenarioContext: 'You are buying tomatoes.',
  xpReward: 10,
  updatedAt: '2026-08-23T12:00:00.000Z',
  phrases: [
    {
      id: 'phrase-1',
      kalangaText: 'Ndinoda tomato',
      englishTranslation: 'I want tomatoes',
      sortOrder: 0,
      variations: [{ id: 'var-1', kalangaText: 'Ndinoda tomato', registerLabel: 'Polite', audio: [] }],
      audio: [
        {
          id: 'audio-1',
          cdnUrl: 'https://cdn.example/a.mp3',
          fileFormat: 'Mp3',
          speakerGender: 'Unspecified',
          dialectLabel: null,
          durationMs: 800,
        },
      ],
    },
  ],
  exercises: [
    {
      id: 'ex-1',
      exerciseType: 'Flashcard',
      promptData: 'Translate: I want tomatoes',
      correctAnswer: 'Ndinoda tomato',
      sortOrder: 0,
    },
  ],
};

describe('LessonContent', () => {
  it('renders title, scenario context, phrases with variations, and exercises', () => {
    const { getByText, getAllByText } = render(<LessonContent lesson={lesson} />);

    expect(getByText('At the market')).toBeTruthy();
    expect(getByText('You are buying tomatoes.')).toBeTruthy();
    expect(getAllByText('Ndinoda tomato').length).toBeGreaterThan(0);
    expect(getByText('I want tomatoes')).toBeTruthy();
    expect(getByText('Polite')).toBeTruthy();
    expect(getByText('1 exercise in this lesson')).toBeTruthy();
    expect(getByText('Play')).toBeTruthy();
  });

  it('starts the lesson exercise sequence', () => {
    const onStartExercises = jest.fn();
    const { getByLabelText } = render(
      <LessonContent lesson={lesson} onStartExercises={onStartExercises} />,
    );

    fireEvent.press(getByLabelText('Start exercises'));
    expect(onStartExercises).toHaveBeenCalled();
  });

  it('offers pronunciation practice for a phrase', () => {
    const onPractisePhrase = jest.fn();
    const { getByText } = render(
      <LessonContent lesson={lesson} onPractisePhrase={onPractisePhrase} />,
    );

    fireEvent.press(getByText('Practise this phrase'));
    expect(onPractisePhrase).toHaveBeenCalledWith('phrase-1');
  });
});
