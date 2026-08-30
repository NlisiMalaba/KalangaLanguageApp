import { transmitPronunciationIfConsented } from '@/domain/pronunciation/transmitPronunciation';

describe('transmitPronunciationIfConsented', () => {
  it('does not call the network when consent is missing', async () => {
    const upload = jest.fn(async () => undefined);
    await expect(
      transmitPronunciationIfConsented('file:///docs/offline/pronunciation/a.m4a', false, { upload }),
    ).resolves.toBe('skipped');
    expect(upload).not.toHaveBeenCalled();
  });

  it('uploads only after explicit consent', async () => {
    const upload = jest.fn(async () => undefined);
    await expect(
      transmitPronunciationIfConsented('file:///docs/offline/pronunciation/a.m4a', true, { upload }),
    ).resolves.toBe('uploaded');
    expect(upload).toHaveBeenCalledTimes(1);
  });
});
