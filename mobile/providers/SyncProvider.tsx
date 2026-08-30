import { AppState, type AppStateStatus } from 'react-native';
import { useEffect, type PropsWithChildren } from 'react';
import * as Network from 'expo-network';

import { useAuth } from '@/ctx/AuthContext';
import { createDefaultSyncService } from '@/lib/createSyncService';
import type { SyncService } from '@/domain/sync';

const defaultSync = createDefaultSyncService();

function isReachable(state: { isConnected?: boolean | null; isInternetReachable?: boolean | null }): boolean {
  if (state.isInternetReachable !== null && state.isInternetReachable !== undefined) {
    return state.isInternetReachable;
  }

  return state.isConnected === true;
}

export default function SyncProvider({
  children,
  syncService = defaultSync,
}: PropsWithChildren<{ syncService?: SyncService }>) {
  const { user } = useAuth();

  useEffect(() => {
    if (!user) {
      return;
    }

    const run = () => {
      void syncService.sync(user.languageId, user.id).catch(() => undefined);
    };

    run();

    const onAppState = (status: AppStateStatus) => {
      if (status === 'active') {
        run();
      }
    };

    const appSubscription = AppState.addEventListener('change', onAppState);
    const addListener = (
      Network as typeof Network & {
        addNetworkStateListener?: (listener: (state: { isConnected?: boolean | null; isInternetReachable?: boolean | null }) => void) => {
          remove: () => void;
        };
      }
    ).addNetworkStateListener;

    const networkSubscription =
      typeof addListener === 'function'
        ? addListener((state) => {
            if (isReachable(state)) {
              run();
            }
          })
        : undefined;

    return () => {
      appSubscription.remove();
      networkSubscription?.remove();
    };
  }, [syncService, user]);

  return children;
}
