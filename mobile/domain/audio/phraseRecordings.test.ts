import { recordingsForPhrase } from '@/domain/audio/phraseRecordings';
import type { AudioRef, LessonPhraseDetail } from '@/domain/catalog/types';

const rec = (id: string): AudioRef => ({
  id,
  cdnUrl: `https://cdn.example/${id}.mp3`,
  fileFormat: 'Mp3',
  speakerGender: 'Unspecified',
  dialectLabel: null,
  durationMs: 800,
});

describe('recordingsForPhrase', () => {
  it('includes phrase and variation audio without duplicates', () => {
    const shared = rec('shared');
    const phrase: LessonPhraseDetail = {
      id: 'phrase-1',
      kalangaText: 'Mhoro',
      englishTranslation: 'Hello',
      sortOrder: 0,
      audio: [shared, rec('phrase-only')],
      variations: [
        {
          id: 'var-1',
          kalangaText: 'Mhoro',
          registerLabel: 'Polite',
          audio: [shared, rec('variation-only')],
        },
      ],
    };

    expect(recordingsForPhrase(phrase).map((item) => item.id)).toEqual([
      'shared',
      'phrase-only',
      'variation-only',
    ]);
  });
});
