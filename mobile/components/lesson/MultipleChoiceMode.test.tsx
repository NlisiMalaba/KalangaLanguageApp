import { fireEvent, render } from '@testing-library/react-native';

import { MultipleChoiceMode } from '@/components/lesson/MultipleChoiceMode';

describe('MultipleChoiceMode', () => {
  it('shows four options and reports the selected index', () => {
    const onSelect = jest.fn();
    const { getByLabelText, getByText } = render(
      <MultipleChoiceMode
        prompt="Mhoro"
        options={['Hello', 'Goodbye', 'Please', 'Thanks']}
        selectedIndex={null}
        onSelect={onSelect}
      />,
    );

    expect(getByText('Mhoro')).toBeTruthy();
    fireEvent.press(getByLabelText('Option 2: Goodbye'));
    expect(onSelect).toHaveBeenCalledWith(1);
  });
});
