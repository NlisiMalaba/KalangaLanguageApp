import { fireEvent, render, waitFor } from '@testing-library/react-native';

import { ContentPackDownloadPanel } from '@/components/contentPacks/ContentPackDownloadPanel';
import { Level } from '@/domain/enums';

describe('ContentPackDownloadPanel', () => {
  it('shows live download percent and a storage warning when paused', async () => {
    const downloadPack = jest.fn(async ({ onProgress }) => {
      onProgress?.({ percent: 40, bytesDownloaded: 40, bytesTotal: 100, pausedForStorage: false });
      return {
        packId: 'pack-1',
        percent: 40,
        pausedForStorage: true,
        filesCompleted: 0,
        filesTotal: 1,
      };
    });

    const { getByLabelText, getByText } = render(
      <ContentPackDownloadPanel
        languageId="lang-1"
        packs={[
          {
            packId: 'pack-1',
            languageId: 'lang-1',
            name: 'Beginner Everyday',
            level: Level.Beginner,
            category: 'Everyday',
            version: 1,
            sizeBytes: 100,
            manifestUrl: 'https://cdn.example/m',
          },
        ]}
        downloadPack={downloadPack}
      />,
    );

    fireEvent.press(getByLabelText('Download Beginner Everyday'));
    await waitFor(() => expect(getByLabelText('Download 40 percent')).toBeTruthy());
    expect(getByText('Storage almost full')).toBeTruthy();
  });
});
