import { fireEvent, render } from '@testing-library/react-native';

import { MicrophonePermissionModal } from '@/components/pronunciation/MicrophonePermissionModal';

describe('MicrophonePermissionModal', () => {
  it('offers a settings deep-link when permission is denied', () => {
    const onOpenSettings = jest.fn();
    const onDismiss = jest.fn();
    const { getByLabelText } = render(
      <MicrophonePermissionModal visible onOpenSettings={onOpenSettings} onDismiss={onDismiss} />,
    );

    fireEvent.press(getByLabelText('Open settings'));
    expect(onOpenSettings).toHaveBeenCalled();
    fireEvent.press(getByLabelText('Not now'));
    expect(onDismiss).toHaveBeenCalled();
  });
});
