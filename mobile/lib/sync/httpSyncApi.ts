import { AuthApiError } from '@/domain/auth/errors';
import { DuplicateSyncPushError } from '@/domain/sync/errors';
import type { SyncPushRequest, SyncPushResult } from '@/domain/sync/types';
import { mapSyncPushResult } from '@/lib/sync/mapSyncResponse';
import { apiRequest } from '@/utils/api';

export type SyncApi = {
  push: (request: SyncPushRequest) => Promise<SyncPushResult>;
  pull: (sinceVersion: number) => Promise<SyncPushResult>;
};

export function createHttpSyncApi(): SyncApi {
  return {
    async push(request: SyncPushRequest): Promise<SyncPushResult> {
      try {
        const body = await apiRequest<unknown>('/sync/push', {
          method: 'POST',
          body: {
            clientOperationId: request.clientOperationId,
            progress: request.progress,
            spacedRepetition: request.spacedRepetition,
            gamification: request.gamification
              ? {
                  totalXp: request.gamification.totalXp,
                  currentStreak: request.gamification.currentStreak,
                  longestStreak: request.gamification.longestStreak,
                  lastActivityDate: request.gamification.lastActivityDate,
                  xpDelta: request.gamification.xpDelta,
                  updatedAt: request.gamification.updatedAt,
                }
              : null,
          },
        });
        return mapSyncPushResult(body);
      } catch (error) {
        if (error instanceof AuthApiError && error.status === 409) {
          throw new DuplicateSyncPushError();
        }

        throw error;
      }
    },
    async pull(sinceVersion: number): Promise<SyncPushResult> {
        const body = await apiRequest<unknown>(`/sync/pull?since=${encodeURIComponent(String(sinceVersion))}`, {
          method: 'GET',
        });
        return mapSyncPushResult(body);
    },
  };
}
