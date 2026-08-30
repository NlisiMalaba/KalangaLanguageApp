import * as fc from 'fast-check';

import { localAudioFileUri } from '@/domain/audio/localAudioPath';
import { memoryContentPackDeps } from '@/domain/contentPacks/contentPackDownloadTestHarness';
import { createContentPackDownloader } from '@/domain/contentPacks/downloadContentPackUseCase';

const fileArb = fc
  .tuple(fc.integer({ min: 1, max: 12 }), fc.array(fc.integer({ min: 0, max: 255 }), { minLength: 1, maxLength: 48 }))
  .map(([n, values]) => ({
    id: `rec-${n}`,
    size: values.length,
    bytes: Uint8Array.from(values),
  }));

const filesArb = fc.uniqueArray(fileArb, { selector: (file) => file.id, minLength: 1, maxLength: 3 });

describe('content pack download round-trip', () => {
  // Feature: kalanga-language-app, Property 22: Content Pack Download Round-Trip
  it('persists every manifest audio file with the same bytes as the source', async () => {
    await fc.assert(
      fc.asyncProperty(filesArb, async (files) => {
        const deps = memoryContentPackDeps({ files });
        const download = createContentPackDownloader(deps);
        const result = await download({ languageId: 'lang-1', packId: 'pack-1' });

        expect(result.pausedForStorage).toBe(false);
        expect(result.percent).toBe(100);
        expect(result.filesCompleted).toBe(files.length);
        expect(result.filesTotal).toBe(files.length);
        expect(deps.packs[0]?.id).toBe('pack-1');
        expect(deps.packs[0]?.lessonIds).toEqual(['lesson-1']);

        for (const file of files) {
          const uri = localAudioFileUri('file:///docs/', 'lang-1', file.id, 'Mp3');
          expect(deps.filesOnDisk.get(uri)).toEqual(file.bytes);
          expect(deps.progress.find((row) => row.recordingId === file.id)?.status).toBe('complete');
        }
      }),
      { numRuns: 100 },
    );
  });
});
