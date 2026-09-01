import { useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import { toast } from 'sonner-native';

import { useAuth } from '@/ctx/AuthContext';
import { AuthError } from '@/domain/auth/errors';

type Mode = 'signIn' | 'signUp';

export default function EmailAuth({ onBack }: { onBack: () => void }) {
  const { signIn, signUp } = useAuth();
  const [mode, setMode] = useState<Mode>('signIn');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async () => {
    if (!email || !password || (mode === 'signUp' && !displayName)) {
      toast.error('Please fill in all fields.');
      return;
    }

    setBusy(true);
    try {
      if (mode === 'signIn') {
        await signIn(email, password);
      } else {
        await signUp(email, password, displayName);
      }
    } catch (error) {
      const message = error instanceof AuthError ? error.message : 'Something went wrong. Please try again.';
      toast.error(message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <View style={styles.container}>
      <Pressable onPress={onBack} accessibilityRole="button">
        <Text style={styles.back}>Back</Text>
      </Pressable>
      <Text style={styles.title}>{mode === 'signIn' ? 'Sign in' : 'Create an account'}</Text>
      <Text style={styles.subtitle}>
        {mode === 'signIn'
          ? 'Use your email and password. Offline sign-in works after a successful online login.'
          : 'Registration needs a network connection.'}
      </Text>
      {mode === 'signUp' ? (
        <TextInput
          style={styles.input}
          placeholder="Display name"
          placeholderTextColor="rgba(255,255,255,0.5)"
          value={displayName}
          onChangeText={setDisplayName}
          autoCapitalize="words"
          accessibilityLabel="Display name"
        />
      ) : null}
      <TextInput
        style={styles.input}
        placeholder="Email"
        placeholderTextColor="rgba(255,255,255,0.5)"
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        keyboardType="email-address"
        accessibilityLabel="Email"
      />
      <TextInput
        style={styles.input}
        placeholder="Password"
        placeholderTextColor="rgba(255,255,255,0.5)"
        value={password}
        onChangeText={setPassword}
        secureTextEntry
        autoComplete={mode === 'signIn' ? 'password' : 'new-password'}
        accessibilityLabel="Password"
      />
      <Pressable
        style={[styles.button, busy && styles.buttonDisabled]}
        onPress={() => void submit()}
        disabled={busy}
        accessibilityRole="button"
        accessibilityLabel={mode === 'signIn' ? 'Sign in' : 'Create account'}>
        {busy ? (
          <ActivityIndicator color="#fff" />
        ) : (
          <Text style={styles.buttonText}>{mode === 'signIn' ? 'Sign in' : 'Create account'}</Text>
        )}
      </Pressable>
      <Pressable
        onPress={() => setMode(mode === 'signIn' ? 'signUp' : 'signIn')}
        accessibilityRole="button"
        accessibilityLabel={mode === 'signIn' ? 'Switch to create account' : 'Switch to sign in'}>
        <Text style={styles.switch}>
          {mode === 'signIn' ? 'Need an account? Create one' : 'Already have an account? Sign in'}
        </Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
  },
  back: {
    color: 'rgba(255,255,255,0.8)',
    fontSize: 16,
    marginBottom: 8,
  },
  title: {
    color: '#fff',
    fontSize: 24,
    fontWeight: '700',
  },
  subtitle: {
    color: 'rgba(255,255,255,0.7)',
    fontSize: 15,
    marginBottom: 8,
  },
  input: {
    backgroundColor: 'rgba(255,255,255,0.1)',
    borderColor: 'rgba(255,255,255,0.2)',
    borderWidth: 1,
    borderRadius: 8,
    paddingVertical: 14,
    paddingHorizontal: 16,
    fontSize: 16,
    color: '#fff',
  },
  button: {
    backgroundColor: '#0a7ea4',
    borderRadius: 8,
    paddingVertical: 14,
    alignItems: 'center',
    minHeight: 48,
    justifyContent: 'center',
  },
  buttonDisabled: {
    opacity: 0.6,
  },
  buttonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  switch: {
    color: 'rgba(255,255,255,0.85)',
    textAlign: 'center',
    marginTop: 8,
    fontSize: 15,
  },
});
