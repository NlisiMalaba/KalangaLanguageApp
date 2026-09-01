import { fireEvent, render } from '@testing-library/react-native';

import { ExerciseBuilder } from '@/components/contributor/ExerciseBuilder';
import { emptyExercise } from '@/domain/contributor/validateLessonDraft';
import { ExerciseType } from '@/domain/enums';

describe('ExerciseBuilder', () => {
  it('adds an exercise and captures multiple-choice options', () => {
    const onChange = jest.fn();
    const { getByLabelText } = render(<ExerciseBuilder exercises={[]} onChange={onChange} />);
    fireEvent.press(getByLabelText('Add exercise'));
    expect(onChange).toHaveBeenCalled();

    const exercise = {
      ...emptyExercise(ExerciseType.MultipleChoice),
      clientKey: 'ex-1',
      prompt: 'Pick',
      options: ['A', 'B', 'C', 'D'],
    };
    const { getByLabelText: get } = render(
      <ExerciseBuilder exercises={[exercise]} onChange={onChange} />,
    );
    fireEvent.changeText(get('Exercise 1 option 2'), 'Bee');
    expect(onChange).toHaveBeenCalledWith([
      expect.objectContaining({ options: ['A', 'Bee', 'C', 'D'] }),
    ]);
  });
});
