import { fireEvent, render, waitFor } from '@testing-library/react-native';

import ProfileScreen from '@/app/(tabs)/profile';
import { AuthContext, type AuthContextValue } from '@/ctx/AuthContext';
import { Level, Role } from '@/domain/enums';
import type { GetProgressResult } from '@/domain/progress/types';

jest.mock('expo-router', () => ({
  useFocusEffect: (effect: () => void | (() => void)) => {
    const { useEffect } = require('react') as typeof import('react');
    useEffect(() => {
      return effect();
    }, [effect]);
  },
}));

jest.mock('@/hooks/useSpeakingListeningStats', () => ({
  useSpeakingListeningStats: () => ({
    speakingMinutes: 1.5,
    listeningMinutes: 0.5,
    reload: jest.fn(),
    recordSpeaking: jest.fn(),
    recordListening: jest.fn(),
  }),
}));

const progress: GetProgressResult = {
  userId: 'user-1',
  totalXp: 40,
  currentStreak: 2,
  longestStreak: 4,
  progressLevel: Level.Beginner,
  lastActivityDate: '2026-08-22',
  byLevel: [
    {
      level: Level.Beginner,
      category: null,
      completedCount: 1,
      totalCount: 2,
      percentage: 50,
    },
  ],
  byCategory: [
    {
      level: null,
      category: 'Everyday',
      completedCount: 1,
      totalCount: 2,
      percentage: 50,
    },
  ],
  weakAreas: [
    {
      level: Level.Beginner,
      category: 'Everyday',
      averageScore: 40,
      sampleSize: 1,
    },
  ],
};

const auth: AuthContextValue = {
  user: {
    id: 'user-1',
    languageId: 'lang-1',
    email: 'learner@example.test',
    displayName: 'Ada',
    role: Role.Learner,
  },
  profile: null,
  loading: false,
  signIn: jest.fn(),
  signUp: jest.fn(),
  signOut: jest.fn(),
  refreshProfile: jest.fn(),
};

describe('ProfileScreen', () => {
  it('shows XP, streaks, completion bars, and weak areas', async () => {
    const getProgress = jest.fn(async () => progress);
    const { getByText, getByLabelText } = render(
      <AuthContext.Provider value={auth}>
        <ProfileScreen getProgress={getProgress} />
      </AuthContext.Provider>,
    );

    await waitFor(() => expect(getByText('40 XP')).toBeTruthy());
    expect(getByText('Ada')).toBeTruthy();
    expect(getByLabelText('Streak 2 days')).toBeTruthy();
    expect(getByText('Level: Beginner')).toBeTruthy();
    expect(getByText('1.5 min speaking')).toBeTruthy();
    expect(getByText('0.5 min listening')).toBeTruthy();
    expect(getByText('Beginner · 1/2 (50%)')).toBeTruthy();
    expect(getByText('Everyday · 1/2 (50%)')).toBeTruthy();
    expect(getByText('Beginner · Everyday — avg 40 (1 score)')).toBeTruthy();

    fireEvent.press(getByLabelText('Sign out'));
    expect(auth.signOut).toHaveBeenCalled();
  });
});
