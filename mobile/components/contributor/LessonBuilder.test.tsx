import { fireEvent, render } from '@testing-library/react-native';

import { LessonBuilder } from '@/components/contributor/LessonBuilder';
import { emptyLessonDraft } from '@/domain/contributor/validateLessonDraft';

describe('LessonBuilder', () => {
  it('edits title, adds a phrase, and saves', () => {
    const onChange = jest.fn();
    const onSave = jest.fn();
    const draft = emptyLessonDraft();
    const { getByLabelText, rerender } = render(
      <LessonBuilder draft={draft} onChange={onChange} onSave={onSave} onSubmit={jest.fn()} />,
    );

    fireEvent.changeText(getByLabelText('Lesson title'), 'Greetings');
    expect(onChange).toHaveBeenCalledWith(expect.objectContaining({ title: 'Greetings' }));

    fireEvent.press(getByLabelText('Add phrase'));
    expect(onChange).toHaveBeenCalledWith(expect.objectContaining({ phrases: expect.any(Array) }));

    const withPhrase = {
      ...draft,
      title: 'Greetings',
      phrases: [
        {
          clientKey: 'p-1',
          id: null,
          kalangaText: 'Mhoro',
          englishTranslation: 'Hello',
          sortOrder: 0,
          variations: [],
          audio: [],
        },
      ],
    };
    rerender(<LessonBuilder draft={withPhrase} onChange={onChange} onSave={onSave} onSubmit={jest.fn()} />);
    expect(getByLabelText('Phrase 1 Kalanga').props.value).toBe('Mhoro');

    fireEvent.press(getByLabelText('Save draft'));
    expect(onSave).toHaveBeenCalled();
  });
});
