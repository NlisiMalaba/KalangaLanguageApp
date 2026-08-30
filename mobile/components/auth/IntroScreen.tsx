import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import EmailAuth from '@/components/auth/EmailAuth';

export default function IntroScreen() {
  const insets = useSafeAreaInsets();
  const [showEmail, setShowEmail] = useState(false);

  return (
    <View style={[styles.root, { paddingTop: insets.top + 48, paddingBottom: insets.bottom + 24 }]}>
      <Text style={styles.kicker}>TjiKalanga</Text>
      <Text style={styles.title}>Learn Kalanga.{'\n'}Keep it alive.</Text>
      <Text style={styles.body}>
        Lessons, native audio, and practice that work even when the network does not.
      </Text>
      <View style={styles.sheet}>
        {showEmail ? (
          <EmailAuth onBack={() => setShowEmail(false)} />
        ) : (
          <>
            <Text style={styles.sheetTitle}>Start today</Text>
            <Pressable
              style={styles.cta}
              onPress={() => setShowEmail(true)}
              accessibilityRole="button"
              accessibilityLabel="Continue with email">
              <Text style={styles.ctaText}>Continue with email</Text>
            </Pressable>
          </>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: '#11181C',
    paddingHorizontal: 24,
    justifyContent: 'space-between',
  },
  kicker: {
    color: '#0a7ea4',
    fontSize: 16,
    fontWeight: '600',
    letterSpacing: 1,
  },
  title: {
    color: '#fff',
    fontSize: 36,
    fontWeight: '800',
    lineHeight: 42,
    marginTop: 12,
  },
  body: {
    color: 'rgba(255,255,255,0.75)',
    fontSize: 17,
    lineHeight: 24,
    marginTop: 16,
    flex: 1,
  },
  sheet: {
    backgroundColor: 'rgba(255,255,255,0.08)',
    borderRadius: 20,
    padding: 24,
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.12)',
  },
  sheetTitle: {
    color: '#fff',
    fontSize: 18,
    fontWeight: '700',
    marginBottom: 16,
  },
  cta: {
    backgroundColor: '#0a7ea4',
    borderRadius: 12,
    paddingVertical: 14,
    alignItems: 'center',
  },
  ctaText: {
    color: '#fff',
    fontSize: 17,
    fontWeight: '600',
  },
});
