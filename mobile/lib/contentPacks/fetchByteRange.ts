import { CONTENT_PACK_DOWNLOAD_TIMEOUT_MS } from '@/constants/contentPacks';
import { ContentPackDownloadError } from '@/domain/contentPacks/errors';
import type { RangeFetchResult } from '@/domain/contentPacks/downloadContentPackUseCase';

export async function fetchByteRange(url: string, startByte: number): Promise<RangeFetchResult> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), CONTENT_PACK_DOWNLOAD_TIMEOUT_MS);
  const headers: Record<string, string> = {};
  if (startByte > 0) {
    headers.Range = `bytes=${startByte}-`;
  }

  try {
    const response = await fetch(url, { method: 'GET', headers, signal: controller.signal });
    if (response.status !== 200 && response.status !== 206) {
      throw new ContentPackDownloadError(`Audio download failed (${response.status}).`);
    }

    const buffer = await response.arrayBuffer();
    return { status: response.status, body: new Uint8Array(buffer) };
  } catch (error) {
    if (error instanceof ContentPackDownloadError) {
      throw error;
    }

    throw new ContentPackDownloadError('Could not download audio. Check your connection and try again.');
  } finally {
    clearTimeout(timeout);
  }
}
