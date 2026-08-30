import * as FileSystem from 'expo-file-system/legacy';

import { ContentPackDownloadError } from '@/domain/contentPacks/errors';

function toBase64(bytes: Uint8Array): string {
  if (typeof Buffer !== 'undefined') {
    return Buffer.from(bytes).toString('base64');
  }

  let binary = '';
  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }

  return btoa(binary);
}

function fromBase64(value: string): Uint8Array {
  if (typeof Buffer !== 'undefined') {
    return new Uint8Array(Buffer.from(value, 'base64'));
  }

  const binary = atob(value);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i);
  }

  return bytes;
}

export function expoDocumentDirectory(): string {
  return FileSystem.documentDirectory ?? 'file:///';
}

export async function expoFreeDiskBytes(): Promise<number> {
  if (typeof FileSystem.getFreeDiskStorageAsync !== 'function') {
    return Number.MAX_SAFE_INTEGER;
  }

  return FileSystem.getFreeDiskStorageAsync();
}

export async function expoEnsureDirectory(directoryUri: string): Promise<void> {
  const info = await FileSystem.getInfoAsync(directoryUri);
  if (info.exists) {
    return;
  }

  await FileSystem.makeDirectoryAsync(directoryUri, { intermediates: true });
}

export async function expoFileSize(fileUri: string): Promise<number> {
  const info = await FileSystem.getInfoAsync(fileUri);
  if (!info.exists || typeof info.size !== 'number') {
    return 0;
  }

  return info.size;
}

export async function expoReadFileBytes(fileUri: string): Promise<Uint8Array> {
  try {
    const encoded = await FileSystem.readAsStringAsync(fileUri, {
      encoding: FileSystem.EncodingType.Base64,
    });
    return fromBase64(encoded);
  } catch {
    throw new ContentPackDownloadError('Could not read the local audio file.');
  }
}

export async function expoWriteFileBytes(fileUri: string, bytes: Uint8Array): Promise<void> {
  await FileSystem.writeAsStringAsync(fileUri, toBase64(bytes), {
    encoding: FileSystem.EncodingType.Base64,
  });
}

export async function expoDeleteAudioFile(fileUri: string): Promise<void> {
  await FileSystem.deleteAsync(fileUri, { idempotent: true });
}
