import { render } from '@testing-library/react-native';

import { ConversationView } from '@/components/conversation/ConversationView';
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
      variations: [],
      audio: [],
    },
  ],
  exercises: [
    {
      id: 'ex-1',
      exerciseType: 'Flashcard',
      promptData: '{}',
      correctAnswer: 'Hello',
      sortOrder: 0,
    },
  ],
};

describe('ConversationView', () => {
  it('leads with the situation then phrases and exercises', () => {
    const { getByLabelText, getByText } = render(<ConversationView lesson={lesson} />);
    expect(getByText('At the market')).toBeTruthy();
    expect(getByLabelText('Situation context')).toBeTruthy();
    expect(getByText('You are buying tomatoes.')).toBeTruthy();
    expect(getByText('Ndinoda tomato')).toBeTruthy();
    expect(getByText('I want tomatoes')).toBeTruthy();
    expect(getByText('1 exercise in this lesson')).toBeTruthy();
  });
});
