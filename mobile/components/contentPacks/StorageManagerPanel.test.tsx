import { fireEvent, render } from '@testing-library/react-native';

import { StorageManagerPanel } from '@/components/contentPacks/StorageManagerPanel';

describe('StorageManagerPanel', () => {
  it('shows total usage, per-pack size, updates, and delete', () => {
    const onDeletePack = jest.fn();
    const { getByText, getByLabelText } = render(
      <StorageManagerPanel
        summary={{
          totalBytes: 1536,
          packs: [
            {
              packId: 'pack-1',
              name: 'Beginner Everyday',
              sizeBytes: 1536,
              version: 1,
              remoteVersion: 2,
              updateAvailable: true,
            },
          ],
        }}
        onDeletePack={onDeletePack}
      />,
    );

    expect(getByLabelText('Total storage 1.5 KB')).toBeTruthy();
    expect(getByText('1.5 KB')).toBeTruthy();
    expect(getByLabelText('Beginner Everyday update available')).toBeTruthy();
    fireEvent.press(getByLabelText('Delete Beginner Everyday'));
    expect(onDeletePack).toHaveBeenCalledWith('pack-1');
  });
});
