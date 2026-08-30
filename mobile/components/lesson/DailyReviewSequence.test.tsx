import { fireEvent, render, waitFor } from '@testing-library/react-native';

import { DailyReviewSequence } from '@/components/lesson/DailyReviewSequence';
import type { ReviewPrompt } from '@/domain/progress/dailyReview';
import type { SpacedRepetitionEngine } from '@/domain/srs/spacedRepetitionEngine';

const prompt: ReviewPrompt = {
  kalangaText: 'Mhoro',
  phrase: {
    id: 'phrase-1',
    languageId: 'lang-1',
    lessonId: 'lesson-1',
    kalangaText: 'Mhoro',
    englishTranslation: 'Hello',
    sortOrder: 0,
    createdAt: '2026-08-22T12:00:00.000Z',
  },
  card: {
    id: 'srs-1',
    languageId: 'lang-1',
    userId: 'user-1',
    phraseId: 'phrase-1',
    variationId: null,
    easeFactor: 2.5,
    intervalDays: 1,
    repetitions: 0,
    nextReviewAt: '2026-08-22',
    lastReviewedAt: null,
    updatedAt: '2026-08-22T12:00:00.000Z',
  },
};

describe('DailyReviewSequence', () => {
  it('scores a due phrase and records the SRS answer', async () => {
    const srs: SpacedRepetitionEngine = {
      recordAnswer: jest.fn(async (input) => ({ ...prompt.card, repetitions: input.isCorrect ? 1 : 0 })),
      getDailyReview: jest.fn(async () => []),
    };
    const { getByLabelText, getByText } = render(
      <DailyReviewSequence
        languageId="lang-1"
        userId="user-1"
        loadPrompts={async () => [prompt]}
        srs={srs}
        onContinue={jest.fn()}
      />,
    );

    await waitFor(() => expect(getByText('Mhoro')).toBeTruthy());
    fireEvent.changeText(getByLabelText('Translation'), 'Hello');
    fireEvent.press(getByLabelText('Check translation'));
    await waitFor(() => expect(srs.recordAnswer).toHaveBeenCalledWith(
      expect.objectContaining({ phraseId: 'phrase-1', isCorrect: true }),
    ));
    fireEvent.press(getByLabelText('Continue'));
    await waitFor(() => expect(getByText('Daily review')).toBeTruthy());
  });
});
