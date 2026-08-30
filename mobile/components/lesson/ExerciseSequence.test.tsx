import { fireEvent, render, waitFor } from '@testing-library/react-native';

import { ExerciseSequence } from '@/components/lesson/ExerciseSequence';
import type { LessonDetail } from '@/domain/catalog/types';
import { ExerciseType, Level } from '@/domain/enums';
import type { ExerciseEngine } from '@/domain/exercises/exerciseEngine';

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
      audio: [],
    },
  ],
  exercises: [
    {
      id: 'ex-1',
      exerciseType: ExerciseType.Flashcard,
      promptData: JSON.stringify({ phrase_id: 'phrase-1', kalanga_text: 'Mhoro' }),
      correctAnswer: 'Hello',
      sortOrder: 0,
    },
    {
      id: 'ex-2',
      exerciseType: ExerciseType.MultipleChoice,
      promptData: JSON.stringify({
        phrase_id: 'phrase-1',
        prompt: 'Mhoro',
        options: ['Hello', 'Goodbye', 'Please', 'Thanks'],
        correct_index: 0,
      }),
      correctAnswer: 'Hello',
      sortOrder: 1,
    },
  ],
};

describe('ExerciseSequence', () => {
  it('runs each exercise then shows the completion screen', async () => {
    const engine: ExerciseEngine = {
      loadLessonExercises: jest.fn(async () => []),
      submit: jest.fn(async ({ attempt }) => ({
        correct: attempt.kind === ExerciseType.Flashcard,
        revealedAnswer: 'Hello',
        phraseId: 'phrase-1',
      })),
    };
    const onContinue = jest.fn();
    const { getByLabelText, getByText } = render(
      <ExerciseSequence lesson={lesson} userId="user-1" engine={engine} onContinue={onContinue} />,
    );

    expect(getByLabelText('Exercise 1 of 2')).toBeTruthy();
    fireEvent.changeText(getByLabelText('Translation'), 'Hello');
    fireEvent.press(getByLabelText('Check translation'));
    await waitFor(() => expect(getByLabelText('Correct')).toBeTruthy());
    fireEvent.press(getByLabelText('Continue'));

    await waitFor(() => expect(getByLabelText('Exercise 2 of 2')).toBeTruthy());
    fireEvent.press(getByLabelText('Option 2: Goodbye'));
    await waitFor(() => expect(getByLabelText('Incorrect')).toBeTruthy());
    expect(engine.submit).toHaveBeenCalledWith(
      expect.objectContaining({
        userId: 'user-1',
        attempt: { kind: ExerciseType.MultipleChoice, selectedIndex: 1 },
      }),
    );
    fireEvent.press(getByLabelText('Continue'));

    await waitFor(() => expect(getByText('Lesson complete')).toBeTruthy());
    expect(getByLabelText('1 of 2 correct')).toBeTruthy();
    fireEvent.press(getByLabelText('Continue'));
    expect(onContinue).toHaveBeenCalled();
  });
});
