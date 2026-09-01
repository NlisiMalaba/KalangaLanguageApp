import { fireEvent, render, waitFor } from '@testing-library/react-native';

import ConversationsScreen from '@/app/(tabs)/conversations';
import { AuthContext, type AuthContextValue } from '@/ctx/AuthContext';
import { LessonDownloadStatus, type CatalogLessonItem } from '@/domain/catalog/types';
import { Level, RequestStatus, Role } from '@/domain/enums';
import type { CommunityRequest } from '@/domain/requests';

const mockPush = jest.fn();

jest.mock('expo-router', () => ({
  router: { push: (...args: unknown[]) => mockPush(...args) },
  useFocusEffect: (effect: () => void | (() => void)) => {
    const { useEffect } = require('react') as typeof import('react');
    useEffect(() => {
      return effect();
    }, [effect]);
  },
}));

const scenario: CatalogLessonItem = {
  id: 'lesson-1',
  languageId: 'lang-1',
  title: 'At the market',
  level: Level.Beginner,
  category: 'Everyday',
  isScenario: true,
  scenarioContext: 'Buying food',
  xpReward: 10,
  updatedAt: '2026-08-23T12:00:00.000Z',
  isCompleted: false,
  downloadStatus: LessonDownloadStatus.NotDownloaded,
};

const openRequest: CommunityRequest = {
  id: 'req-1',
  languageId: 'lang-1',
  submitterId: 'user-1',
  title: 'Airport',
  description: 'Need check-in phrases',
  upvoteCount: 2,
  status: RequestStatus.Open,
  fulfilledByLessonId: null,
  createdAt: '2026-08-23T12:00:00.000Z',
  updatedAt: '2026-08-23T12:00:00.000Z',
};

const learner: AuthContextValue = {
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

describe('ConversationsScreen', () => {
  beforeEach(() => {
    mockPush.mockClear();
  });

  it('shows scenario lessons and requests sorted by upvotes', async () => {
    const { getByText, getByLabelText } = render(
      <AuthContext.Provider value={learner}>
        <ConversationsScreen
          browseCatalog={jest.fn(async () => [scenario])}
          listRequests={jest.fn(async () => [
            { ...openRequest, id: 'low', title: 'Quiet', upvoteCount: 0 },
            openRequest,
          ])}
          submitRequest={jest.fn()}
          upvoteRequest={jest.fn()}
          fulfillRequest={jest.fn()}
        />
      </AuthContext.Provider>,
    );

    await waitFor(() => expect(getByText('At the market')).toBeTruthy());
    expect(getByText('Airport')).toBeTruthy();
    expect(getByLabelText('2 upvotes')).toBeTruthy();

    fireEvent.press(getByLabelText(/At the market/));
    expect(mockPush).toHaveBeenCalledWith({ pathname: '/conversation', params: { id: 'lesson-1' } });
  });

  it('submits a request and upvotes it', async () => {
    const submitRequest = jest.fn(async () => ({ ...openRequest, id: 'req-2', title: 'Clinic', upvoteCount: 0 }));
    const upvoteRequest = jest.fn(async () => ({
      request: { ...openRequest, upvoteCount: 3 },
      applied: true,
    }));
    const { getByLabelText, getByText } = render(
      <AuthContext.Provider value={learner}>
        <ConversationsScreen
          browseCatalog={jest.fn(async () => [])}
          listRequests={jest.fn(async () => [openRequest])}
          submitRequest={submitRequest}
          upvoteRequest={upvoteRequest}
          fulfillRequest={jest.fn()}
        />
      </AuthContext.Provider>,
    );

    await waitFor(() => expect(getByText('Airport')).toBeTruthy());
    fireEvent.changeText(getByLabelText('Request title'), 'Clinic');
    fireEvent.changeText(getByLabelText('Request description'), 'Need doctor phrases');
    fireEvent.press(getByLabelText('Submit request'));
    await waitFor(() => expect(submitRequest).toHaveBeenCalled());
    await waitFor(() => expect(getByText('Clinic')).toBeTruthy());
    fireEvent.press(getByLabelText('Upvote Airport'));
    await waitFor(() => expect(upvoteRequest).toHaveBeenCalled());
  });
});
