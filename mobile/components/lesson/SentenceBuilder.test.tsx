import { fireEvent, render } from '@testing-library/react-native';
import { View } from 'react-native';

import { SentenceBuilder } from '@/components/lesson/SentenceBuilder';

jest.mock('react-native-gesture-handler', () => {
  const React = require('react');
  const { View: RNView } = require('react-native');
  return {
    GestureHandlerRootView: ({ children }: { children: React.ReactNode }) =>
      React.createElement(RNView, null, children),
    GestureDetector: ({ children }: { children: React.ReactNode }) => children,
    Gesture: {
      Pan: () => ({
        enabled() {
          return this;
        },
        onEnd() {
          return this;
        },
      }),
    },
  };
});

describe('SentenceBuilder', () => {
  it('assembles tokens and submits their order', () => {
    const onSubmit = jest.fn();
    const { getByLabelText } = render(
      <View>
        <SentenceBuilder prompt="I want tomatoes" tokens={['tomato', 'Ndinoda']} onSubmit={onSubmit} />
      </View>,
    );

    fireEvent.press(getByLabelText('Word token Ndinoda'));
    fireEvent.press(getByLabelText('Word token tomato'));
    fireEvent.press(getByLabelText('Check sentence'));
    expect(onSubmit).toHaveBeenCalledWith([1, 0]);
  });
});
