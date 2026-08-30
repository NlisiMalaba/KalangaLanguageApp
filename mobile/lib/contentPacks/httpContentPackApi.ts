import { AuthApiError } from '@/domain/auth/errors';
import {
  ContentPackApiError,
  ContentPackNotFoundError,
} from '@/domain/contentPacks/errors';
import type { ContentPackListItem, ContentPackManifest, ManifestAudio } from '@/domain/contentPacks/types';
import type { EntityId } from '@/domain/entities';
import { Level } from '@/domain/enums';
import { mapLevel, requireNumber, requireString, unwrapId, type IdWire } from '@/lib/catalog/wire';
import { apiRequest } from '@/utils/api';

type PackWire = {
  packId?: IdWire;
  languageId?: IdWire;
  name?: unknown;
  level?: unknown;
  category?: unknown;
  version?: unknown;
  sizeBytes?: unknown;
  manifestUrl?: unknown;
};

type ListWire = {
  packs?: PackWire[];
};

type AudioWire = {
  audioRecordingId?: IdWire;
  cdnUrl?: unknown;
  fileFormat?: unknown;
  fileSizeBytes?: unknown;
};

type LessonWire = {
  lessonId?: IdWire;
  audio?: AudioWire[];
};

type ManifestWire = {
  packId?: IdWire;
  languageId?: IdWire;
  name?: unknown;
  version?: unknown;
  level?: unknown;
  category?: unknown;
  generatedAt?: unknown;
  sizeBytes?: unknown;
  lessons?: LessonWire[];
};

function mapOptionalLevel(value: unknown): Level | null {
  if (value == null || value === '') {
    return null;
  }

  return mapLevel(value);
}

function mapAudio(item: AudioWire): ManifestAudio {
  return {
    audioRecordingId: unwrapId(item.audioRecordingId, 'audioRecordingId'),
    cdnUrl: requireString(item.cdnUrl, 'cdnUrl'),
    fileFormat: requireString(item.fileFormat, 'fileFormat'),
    fileSizeBytes: requireNumber(item.fileSizeBytes, 'fileSizeBytes'),
  };
}

export function mapListContentPacksResponse(body: unknown): ContentPackListItem[] {
  const packs = (body as ListWire | null)?.packs;
  if (!Array.isArray(packs)) {
    throw new ContentPackApiError('Content pack list is missing packs.');
  }

  return packs.map((item) => ({
    packId: unwrapId(item.packId, 'packId'),
    languageId: unwrapId(item.languageId, 'languageId'),
    name: requireString(item.name, 'name'),
    level: mapOptionalLevel(item.level),
    category: typeof item.category === 'string' ? item.category : null,
    version: requireNumber(item.version, 'version'),
    sizeBytes: requireNumber(item.sizeBytes, 'sizeBytes'),
    manifestUrl: requireString(item.manifestUrl, 'manifestUrl'),
  }));
}

export function mapContentPackManifestResponse(body: unknown): ContentPackManifest {
  const wire = (body ?? {}) as ManifestWire;
  const lessons = wire.lessons;
  if (!Array.isArray(lessons)) {
    throw new ContentPackApiError('Content pack manifest is missing lessons.');
  }

  return {
    packId: unwrapId(wire.packId, 'packId'),
    languageId: unwrapId(wire.languageId, 'languageId'),
    name: requireString(wire.name, 'name'),
    version: requireNumber(wire.version, 'version'),
    level: mapOptionalLevel(wire.level),
    category: typeof wire.category === 'string' ? wire.category : null,
    generatedAt: requireString(wire.generatedAt, 'generatedAt'),
    sizeBytes: requireNumber(wire.sizeBytes, 'sizeBytes'),
    lessons: lessons.map((lesson) => ({
      lessonId: unwrapId(lesson.lessonId, 'lessonId'),
      audio: Array.isArray(lesson.audio) ? lesson.audio.map(mapAudio) : [],
    })),
  };
}

export type ContentPackApi = {
  listPacks: (languageId: EntityId) => Promise<ContentPackListItem[]>;
  getManifest: (languageId: EntityId, packId: EntityId) => Promise<ContentPackManifest>;
};

export function createHttpContentPackApi(): ContentPackApi {
  return {
    async listPacks(languageId) {
      const body = await apiRequest<unknown>('/content-packs', { method: 'GET' });
      return mapListContentPacksResponse(body).filter((pack) => pack.languageId === languageId);
    },
    async getManifest(languageId, packId) {
      try {
        const body = await apiRequest<unknown>(`/content-packs/${packId}/manifest`, { method: 'GET' });
        const manifest = mapContentPackManifestResponse(body);
        if (manifest.languageId !== languageId) {
          throw new ContentPackNotFoundError(packId);
        }

        return manifest;
      } catch (error) {
        if (error instanceof AuthApiError && error.status === 404) {
          throw new ContentPackNotFoundError(packId);
        }

        throw error;
      }
    },
  };
}
