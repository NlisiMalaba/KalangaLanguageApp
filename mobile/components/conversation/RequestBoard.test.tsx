import { fireEvent, render } from '@testing-library/react-native';

import { RequestBoard } from '@/components/conversation/RequestBoard';
import { LessonDownloadStatus } from '@/domain/catalog/types';
import { Level, RequestStatus } from '@/domain/enums';

const request = {
  id: 'req-1',
  languageId: 'lang-1',
  submitterId: 'user-1',
  title: 'Airport',
  description: 'Need check-in phrases',
  upvoteCount: 4,
  status: RequestStatus.Open,
  fulfilledByLessonId: null,
  createdAt: '2026-08-23T12:00:00.000Z',
  updatedAt: '2026-08-23T12:00:00.000Z',
};

describe('RequestBoard', () => {
  it('submits, upvotes, and lets a contributor link a lesson', () => {
    const onSubmit = jest.fn();
    const onUpvote = jest.fn();
    const onFulfill = jest.fn();
    const { getByLabelText } = render(
      <RequestBoard
        requests={[request]}
        title="Airport"
        description="Need check-in phrases"
        onTitleChange={jest.fn()}
        onDescriptionChange={jest.fn()}
        onSubmit={onSubmit}
        onUpvote={onUpvote}
        onFulfill={onFulfill}
        canFulfill
        fulfillLessons={[
          {
            id: 'lesson-1',
            languageId: 'lang-1',
            title: 'At the airport',
            level: Level.Beginner,
            category: 'Travel',
            isScenario: true,
            scenarioContext: 'Check-in',
            xpReward: 10,
            updatedAt: '2026-08-23T12:00:00.000Z',
            isCompleted: false,
            downloadStatus: LessonDownloadStatus.NotDownloaded,
          },
        ]}
      />,
    );

    fireEvent.press(getByLabelText('Submit request'));
    fireEvent.press(getByLabelText('Upvote Airport'));
    fireEvent.press(getByLabelText('Fulfill Airport with At the airport'));
    expect(onSubmit).toHaveBeenCalled();
    expect(onUpvote).toHaveBeenCalledWith('req-1');
    expect(onFulfill).toHaveBeenCalledWith('req-1', 'lesson-1');
  });
});
