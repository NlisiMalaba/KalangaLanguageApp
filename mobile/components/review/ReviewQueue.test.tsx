import { fireEvent, render } from '@testing-library/react-native';

import { ReviewQueue } from '@/components/review/ReviewQueue';
import { Level } from '@/domain/enums';

describe('ReviewQueue', () => {
  it('approves a pending lesson', () => {
    const onApprove = jest.fn();
    const { getByLabelText } = render(
      <ReviewQueue
        items={[
          {
            lessonId: 'lesson-1',
            title: 'E2E Market',
            level: Level.Beginner,
            category: 'Everyday',
          },
        ]}
        onApprove={onApprove}
      />,
    );

    fireEvent.press(getByLabelText('Approve E2E Market'));
    expect(onApprove).toHaveBeenCalledWith('lesson-1');
  });
});
