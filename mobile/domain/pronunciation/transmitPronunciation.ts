import type { PronunciationTransmitter } from '@/domain/pronunciation/types';

export async function transmitPronunciationIfConsented(
  localUri: string,
  consentGranted: boolean,
  transmitter: PronunciationTransmitter,
): Promise<'skipped' | 'uploaded'> {
  if (!consentGranted) {
    return 'skipped';
  }

  await transmitter.upload(localUri);
  return 'uploaded';
}
