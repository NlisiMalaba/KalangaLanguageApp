import { LOCAL_AUDIO_DIRECTORY } from '@/constants/audio';
import { localAudioFileUri, localAudioRelativePath } from '@/domain/audio/localAudioPath';

describe('localAudioPath', () => {
  it('builds the offline pack path with the recording extension', () => {
    expect(localAudioRelativePath('lang-1', 'rec-1', 'Mp3')).toBe(
      `${LOCAL_AUDIO_DIRECTORY}/lang-1/rec-1.mp3`,
    );
    expect(localAudioFileUri('file:///docs', 'lang-1', 'rec-1', 'Aac')).toBe(
      'file:///docs/offline/audio/lang-1/rec-1.aac',
    );
  });
});
