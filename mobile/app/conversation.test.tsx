import { fireEvent, render, waitFor } from '@testing-library/react-native';

import ConversationScreen from '@/app/conversation';
import type { LessonDetail } from '@/domain/catalog/types';
import { Level } from '@/domain/enums';

const mockPush = jest.fn();

jest.mock('expo-router', () => ({
  router: { push: (...args: unknown[]) => mockPush(...args) },
  useLocalSearchParams: () => ({ id: 'lesson-1' }),
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
      variations: [],
      audio: [],
    },
  ],
  exercises: [
    { id: 'ex-1', exerciseType: 'Flashcard', promptData: '{}', correctAnswer: 'x', sortOrder: 0 },
  ],
};

describe('ConversationScreen', () => {
  it('loads the situation and starts exercises', async () => {
    const loadLesson = jest.fn(async () => lesson);
    const { getByLabelText, getByText } = render(<ConversationScreen loadLesson={loadLesson} />);

    await waitFor(() => expect(getByText('You are buying tomatoes.')).toBeTruthy());
    fireEvent.press(getByLabelText('Start exercises'));
    expect(mockPush).toHaveBeenCalledWith({
      pathname: '/practise',
      params: { lessonId: 'lesson-1', mode: 'exercises' },
    });
  });
});
