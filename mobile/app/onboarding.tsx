import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { toast } from 'sonner-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { useAuth } from '@/ctx/AuthContext';
import { Level } from '@/domain/enums';
import { saveOnboardingProfile } from '@/lib/onboarding';

const LEVELS = [Level.Beginner, Level.Intermediate, Level.Advanced] as const;
const MOTIVATIONS = ['Family', 'Heritage', 'Travel', 'Work', 'Curiosity'] as const;
const INTERESTS = ['Greetings', 'Market', 'Home', 'Stories', 'Songs'] as const;

export default function OnboardingScreen() {
  const { user, refreshProfile } = useAuth();
  const [startingLevel, setStartingLevel] = useState<(typeof LEVELS)[number]>(Level.Beginner);
  const [motivation, setMotivation] = useState<string>(MOTIVATIONS[0]);
  const [interests, setInterests] = useState<string[]>([INTERESTS[0]]);
  const [busy, setBusy] = useState(false);

  const toggleInterest = (interest: string) => {
    setInterests((current) =>
      current.includes(interest) ? current.filter((item) => item !== interest) : [...current, interest],
    );
  };

  const complete = async () => {
    if (!user) {
      router.replace('/');
      return;
    }

    if (interests.length === 0) {
      toast.error('Pick at least one interest.');
      return;
    }

    setBusy(true);
    try {
      await saveOnboardingProfile(user.id, {
        onboardingCompleted: true,
        startingLevel,
        motivation,
        interests,
      });
      await refreshProfile();
      router.replace('/(tabs)/lessons');
    } finally {
      setBusy(false);
    }
  };

  return (
    <ThemedView style={styles.container}>
      <ThemedText type="title">Welcome</ThemedText>
      <ThemedText>Choose a starting level, why you are learning, and a few interests.</ThemedText>

      <ThemedText type="subtitle">Starting level</ThemedText>
      <View style={styles.row}>
        {LEVELS.map((level) => (
          <Chip key={level} label={level} selected={startingLevel === level} onPress={() => setStartingLevel(level)} />
        ))}
      </View>

      <ThemedText type="subtitle">Motivation</ThemedText>
      <View style={styles.row}>
        {MOTIVATIONS.map((item) => (
          <Chip key={item} label={item} selected={motivation === item} onPress={() => setMotivation(item)} />
        ))}
      </View>

      <ThemedText type="subtitle">Interests</ThemedText>
      <View style={styles.row}>
        {INTERESTS.map((item) => (
          <Chip
            key={item}
            label={item}
            selected={interests.includes(item)}
            onPress={() => toggleInterest(item)}
          />
        ))}
      </View>

      <Pressable
        style={[styles.cta, busy && styles.disabled]}
        onPress={() => void complete()}
        disabled={busy}
        accessibilityRole="button">
        <Text style={styles.ctaText}>Continue</Text>
      </Pressable>
    </ThemedView>
  );
}

function Chip({
  label,
  selected,
  onPress,
}: {
  label: string;
  selected: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      onPress={onPress}
      style={[styles.chip, selected && styles.chipSelected]}
      accessibilityRole="button"
      accessibilityState={{ selected }}>
      <Text style={[styles.chipText, selected && styles.chipTextSelected]}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
    gap: 16,
    paddingTop: 64,
  },
  row: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  chip: {
    borderWidth: 1,
    borderColor: '#687076',
    borderRadius: 20,
    paddingHorizontal: 14,
    paddingVertical: 8,
  },
  chipSelected: {
    backgroundColor: '#0a7ea4',
    borderColor: '#0a7ea4',
  },
  chipText: {
    color: '#11181C',
  },
  chipTextSelected: {
    color: '#fff',
  },
  cta: {
    marginTop: 12,
    backgroundColor: '#0a7ea4',
    borderRadius: 12,
    paddingVertical: 14,
    alignItems: 'center',
  },
  disabled: {
    opacity: 0.6,
  },
  ctaText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
});
