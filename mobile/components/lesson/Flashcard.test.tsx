import { fireEvent, render } from '@testing-library/react-native';

import { Flashcard } from '@/components/lesson/Flashcard';

describe('Flashcard', () => {
  it('shows the phrase and submits a recalled translation', () => {
    const onSubmitTranslation = jest.fn();
    const { getByLabelText, getByText } = render(
      <Flashcard kalangaText="Mhoro" onSubmitTranslation={onSubmitTranslation} />,
    );

    expect(getByText('Mhoro')).toBeTruthy();
    fireEvent.changeText(getByLabelText('Translation'), 'Hello');
    fireEvent.press(getByLabelText('Check translation'));
    expect(onSubmitTranslation).toHaveBeenCalledWith('Hello');
  });

  it('lets the learner self-grade after revealing', () => {
    const onSelfGrade = jest.fn();
    const { getByLabelText } = render(
      <Flashcard kalangaText="Mhoro" onSubmitTranslation={jest.fn()} onSelfGrade={onSelfGrade} />,
    );

    fireEvent.press(getByLabelText('Reveal translation'));
    fireEvent.press(getByLabelText('Need review'));
    expect(onSelfGrade).toHaveBeenCalledWith(false);
  });
});
