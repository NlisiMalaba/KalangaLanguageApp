import * as fc from 'fast-check';

import { applyRangeChunk } from '@/domain/contentPacks/bytes';
import { memoryContentPackDeps } from '@/domain/contentPacks/contentPackDownloadTestHarness';
import { createContentPackDownloader } from '@/domain/contentPacks/downloadContentPackUseCase';
import { localAudioFileUri } from '@/domain/audio/localAudioPath';

const payloadArb = fc.array(fc.integer({ min: 0, max: 255 }), { minLength: 2, maxLength: 64 });

describe('resumable download byte integrity', () => {
  // Feature: kalanga-language-app, Property 23: Resumable Download Byte Integrity
  it('concatenates a Range remainder onto a prefix without corrupting bytes', async () => {
    await fc.assert(
      fc.asyncProperty(payloadArb, fc.integer({ min: 1, max: 63 }), async (values, rawOffset) => {
        const bytes = Uint8Array.from(values);
        const offset = 1 + (rawOffset % (bytes.length - 1));
        const prefix = bytes.subarray(0, offset);
        const remainder = bytes.subarray(offset);

        expect(applyRangeChunk(prefix, remainder, offset)).toEqual(bytes);

        const file = { id: 'rec-1', size: bytes.length, bytes };
        const deps = memoryContentPackDeps({
          files: [file],
          seed: { recordingId: 'rec-1', bytes: Uint8Array.from(prefix) },
        });
        const download = createContentPackDownloader(deps);
        const result = await download({ languageId: 'lang-1', packId: 'pack-1' });

        expect(deps.rangeStarts).toEqual([offset]);
        expect(result.percent).toBe(100);
        expect(result.pausedForStorage).toBe(false);
        const uri = localAudioFileUri('file:///docs/', 'lang-1', 'rec-1', 'Mp3');
        expect(deps.filesOnDisk.get(uri)).toEqual(bytes);
        expect(deps.progress[0]?.bytesDownloaded).toBe(bytes.length);
        expect(deps.progress[0]?.status).toBe('complete');
      }),
      { numRuns: 100 },
    );
  });
});
