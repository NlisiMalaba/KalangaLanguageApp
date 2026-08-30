import * as fc from 'fast-check';

import { createPronunciationRecorder } from '@/domain/pronunciation/pronunciationRecorder';
import { scorePronunciation } from '@/domain/pronunciation/pronunciationScorer';
import { transmitPronunciationIfConsented } from '@/domain/pronunciation/transmitPronunciation';

const languageIdArb = fc.uuid();
const durationArb = fc.integer({ min: 0, max: 8_000 });

describe('pronunciation recordings not transmitted without consent', () => {
  // Feature: kalanga-language-app, Property 12: Pronunciation Recordings Not Transmitted Without Consent
  it('keeps a practice session on-device and never calls the API when consent is false', async () => {
    await fc.assert(
      fc.asyncProperty(languageIdArb, durationArb, fc.boolean(), async (languageId, durationMs, consentGranted) => {
        const apiRequest = jest.fn(async (_path: string, _init: { method: string; body: unknown }) => undefined);
        const persist = jest.fn(async (tenant: string, tempUri: string) => {
          return `file:///docs/offline/pronunciation/${tenant}/${tempUri.split('/').pop()}`;
        });
        const start = jest.fn(async () => undefined);
        const stop = jest.fn(async () => ({ tempUri: 'file:///tmp/rec.m4a', durationMs }));

        const recorder = createPronunciationRecorder({
          microphone: {
            getPermission: async () => 'granted',
            requestPermission: async () => 'granted',
          },
          recording: { start, stop },
          store: { persist },
          settings: { openSettings: async () => undefined },
        });

        await recorder.start(languageId);
        const local = await recorder.stop();
        scorePronunciation(
          { durationMs: local.durationMs, energyProfile: local.energyProfile },
          { durationMs: 1_000, energyProfile: [] },
        );

        const outcome = await transmitPronunciationIfConsented(local.uri, consentGranted, {
          upload: async (uri) => {
            await apiRequest('/audio/upload', { method: 'POST', body: { uri } });
          },
        });

        expect(persist).toHaveBeenCalledTimes(1);
        expect(local.uri.startsWith('file:///docs/offline/pronunciation/')).toBe(true);

        if (!consentGranted) {
          expect(outcome).toBe('skipped');
          expect(apiRequest).not.toHaveBeenCalled();
        } else {
          expect(outcome).toBe('uploaded');
          expect(apiRequest).toHaveBeenCalledTimes(1);
        }
      }),
      { numRuns: 100 },
    );
  });
});
