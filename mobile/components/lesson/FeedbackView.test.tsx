import { render } from '@testing-library/react-native';

import { FeedbackView } from '@/components/lesson/FeedbackView';
import { PronunciationLabel } from '@/domain/pronunciation/types';

describe('FeedbackView', () => {
  it('shows the numeric score and qualitative label', () => {
    const { getByLabelText, getByText } = render(
      <FeedbackView score={{ score: 72, label: PronunciationLabel.Good }} />,
    );

    expect(getByText('72')).toBeTruthy();
    expect(getByText('Good')).toBeTruthy();
    expect(getByLabelText('Score 72')).toBeTruthy();
    expect(getByLabelText('Feedback Good')).toBeTruthy();
  });
});
