import { MicrophonePermissionDeniedError, PronunciationRecordingError } from '@/domain/pronunciation/errors';
import { createPronunciationRecorder } from '@/domain/pronunciation/pronunciationRecorder';
import type { PronunciationRecorderDeps } from '@/domain/pronunciation/pronunciationRecorder';

function createDeps(overrides: Partial<PronunciationRecorderDeps> = {}): PronunciationRecorderDeps {
  return {
    microphone: {
      getPermission: async () => 'granted',
      requestPermission: async () => 'granted',
    },
    recording: {
      start: jest.fn(async () => undefined),
      stop: jest.fn(async () => ({ tempUri: 'file:///tmp/rec.m4a', durationMs: 900 })),
    },
    store: {
      persist: jest.fn(async (_languageId, tempUri) => `file:///docs/offline/pronunciation/lang/${tempUri.split('/').pop()}`),
    },
    settings: { openSettings: jest.fn(async () => undefined) },
    ...overrides,
  };
}

describe('PronunciationRecorder', () => {
  it('writes only to local storage after a granted recording', async () => {
    const deps = createDeps();
    const recorder = createPronunciationRecorder(deps);

    await recorder.start('lang-1');
    const result = await recorder.stop();

    expect(deps.recording.start).toHaveBeenCalled();
    expect(deps.store.persist).toHaveBeenCalledWith('lang-1', 'file:///tmp/rec.m4a');
    expect(result.uri.startsWith('file:///docs/offline/pronunciation/')).toBe(true);
    expect(result.durationMs).toBe(900);
  });

  it('does not start recording when the microphone is denied', async () => {
    const deps = createDeps({
      microphone: {
        getPermission: async () => 'denied',
        requestPermission: async () => 'denied',
      },
    });
    const recorder = createPronunciationRecorder(deps);

    await expect(recorder.start('lang-1')).rejects.toBeInstanceOf(MicrophonePermissionDeniedError);
    expect(deps.recording.start).not.toHaveBeenCalled();
    expect(deps.settings.openSettings).not.toHaveBeenCalled();
  });

  it('opens device settings for a denied permission', async () => {
    const deps = createDeps();
    const recorder = createPronunciationRecorder(deps);
    await recorder.openSettings();
    expect(deps.settings.openSettings).toHaveBeenCalled();
  });

  it('rejects stop when nothing is recording', async () => {
    const recorder = createPronunciationRecorder(createDeps());
    await expect(recorder.stop()).rejects.toBeInstanceOf(PronunciationRecordingError);
  });
});
