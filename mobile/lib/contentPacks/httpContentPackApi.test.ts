import { mapContentPackManifestResponse, mapListContentPacksResponse } from '@/lib/contentPacks/httpContentPackApi';

describe('httpContentPackApi mapping', () => {
  it('maps list and manifest payloads', () => {
    const packs = mapListContentPacksResponse({
      packs: [
        {
          packId: 'pack-1',
          languageId: 'lang-1',
          name: 'Beginner Everyday',
          level: 'Beginner',
          category: 'Everyday',
          version: 1,
          sizeBytes: 4096,
          manifestUrl: 'https://cdn.example/m',
        },
      ],
    });
    expect(packs[0]?.packId).toBe('pack-1');
    expect(packs[0]?.sizeBytes).toBe(4096);

    const manifest = mapContentPackManifestResponse({
      packId: { value: 'pack-1' },
      languageId: 'lang-1',
      name: 'Beginner Everyday',
      version: 1,
      generatedAt: '2026-08-22T12:00:00.000Z',
      sizeBytes: 4,
      lessons: [
        {
          lessonId: 'lesson-1',
          audio: [
            {
              audioRecordingId: 'rec-1',
              cdnUrl: 'https://cdn.example/a.mp3',
              fileFormat: 'Mp3',
              fileSizeBytes: 4,
            },
          ],
        },
      ],
    });
    expect(manifest.lessons[0]?.audio[0]?.audioRecordingId).toBe('rec-1');
  });
});
