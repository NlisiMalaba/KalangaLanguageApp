import * as Network from 'expo-network';

import type { NetworkStatus } from '@/domain/auth/ports';

export function createExpoNetworkStatus(): NetworkStatus {
  return {
    async isOnline(): Promise<boolean> {
      const state = await Network.getNetworkStateAsync();
      if (state.isInternetReachable !== null && state.isInternetReachable !== undefined) {
        return state.isInternetReachable;
      }

      return state.isConnected === true;
    },
  };
}
