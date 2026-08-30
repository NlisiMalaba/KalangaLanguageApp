import { fireEvent, render } from '@testing-library/react-native';

import { LessonCompleteScreen } from '@/components/lesson/LessonCompleteScreen';

describe('LessonCompleteScreen', () => {
  it('shows accuracy and lets the learner continue or retry', () => {
    const onContinue = jest.fn();
    const onPracticeAgain = jest.fn();
    const { getByLabelText, getByText } = render(
      <LessonCompleteScreen
        stats={{ title: 'Greetings', xpReward: 10, correctCount: 3, totalCount: 4 }}
        onContinue={onContinue}
        onPracticeAgain={onPracticeAgain}
      />,
    );

    expect(getByText('Lesson complete')).toBeTruthy();
    expect(getByLabelText('Accuracy 75 percent')).toBeTruthy();
    fireEvent.press(getByLabelText('Continue'));
    expect(onContinue).toHaveBeenCalled();
    fireEvent.press(getByLabelText('Practice again'));
    expect(onPracticeAgain).toHaveBeenCalled();
  });
});
