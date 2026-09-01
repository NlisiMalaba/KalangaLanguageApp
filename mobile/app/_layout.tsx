import { DarkTheme, DefaultTheme, ThemeProvider } from '@react-navigation/native';
import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import 'react-native-reanimated';
import { Toaster } from 'sonner-native';

import { useColorScheme } from '@/hooks/use-color-scheme';
import AuthProvider from '@/providers/AuthProvider';
import SyncProvider from '@/providers/SyncProvider';

export const unstable_settings = {
  anchor: '(tabs)',
};

export default function RootLayout() {
  const colorScheme = useColorScheme();

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <AuthProvider>
        <SyncProvider>
          <ThemeProvider value={colorScheme === 'dark' ? DarkTheme : DefaultTheme}>
            <Stack>
              <Stack.Screen name="index" options={{ headerShown: false }} />
              <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
              <Stack.Screen name="onboarding" options={{ headerShown: false }} />
              <Stack.Screen name="lesson/[id]" options={{ title: 'Lesson' }} />
              <Stack.Screen name="practise" options={{ title: 'Practise' }} />
              <Stack.Screen name="contributor/[id]" options={{ title: 'Lesson builder' }} />
              <Stack.Screen name="conversation" options={{ title: 'Conversation' }} />
              <Stack.Screen name="review/queue" options={{ title: 'Review queue' }} />
            </Stack>
            <StatusBar style="auto" />
            <Toaster />
          </ThemeProvider>
        </SyncProvider>
      </AuthProvider>
    </GestureHandlerRootView>
  );
}
