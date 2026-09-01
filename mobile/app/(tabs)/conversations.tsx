import { useCallback, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet, View } from 'react-native';
import { router, useFocusEffect } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';
import { toast } from 'sonner-native';

import { RequestBoard } from '@/components/conversation/RequestBoard';
import { LessonCard } from '@/components/lesson/LessonCard';
import { ThemedText } from '@/components/themed-text';
import { MAX_CATALOG_TAKE } from '@/constants/catalog';
import { getLanguageId } from '@/constants/config';
import { useAuth } from '@/ctx/AuthContext';
import { CatalogError } from '@/domain/catalog/errors';
import type { CatalogLessonItem } from '@/domain/catalog/types';
import { Role } from '@/domain/enums';
import {
  RequestError,
  RequestValidationError,
  createFulfillRequestUseCase,
  createListRequestsUseCase,
  createSubmitRequestUseCase,
  createUpvoteRequestUseCase,
  sortRequestsByUpvoteCount,
  type CommunityRequest,
} from '@/domain/requests';
import { createDefaultBrowseLessonCatalogUseCase } from '@/lib/catalog/createBrowseLessonCatalogUseCase';
import { createHttpRequestsApi } from '@/lib/requests/httpRequestsApi';

const defaultBrowse = createDefaultBrowseLessonCatalogUseCase();
const defaultApi = createHttpRequestsApi();
const defaultList = createListRequestsUseCase(defaultApi);
const defaultSubmit = createSubmitRequestUseCase(defaultApi);
const defaultUpvote = createUpvoteRequestUseCase(defaultApi);
const defaultFulfill = createFulfillRequestUseCase(defaultApi);

export default function ConversationsScreen({
  browseCatalog = defaultBrowse,
  listRequests = defaultList,
  submitRequest = defaultSubmit,
  upvoteRequest = defaultUpvote,
  fulfillRequest = defaultFulfill,
}: {
  browseCatalog?: typeof defaultBrowse;
  listRequests?: typeof defaultList;
  submitRequest?: typeof defaultSubmit;
  upvoteRequest?: typeof defaultUpvote;
  fulfillRequest?: typeof defaultFulfill;
}) {
  const { user } = useAuth();
  const canFulfill = user?.role === Role.Contributor || user?.role === Role.Admin;
  const [scenarios, setScenarios] = useState<CatalogLessonItem[]>([]);
  const [fulfillLessons, setFulfillLessons] = useState<CatalogLessonItem[]>([]);
  const [requests, setRequests] = useState<CommunityRequest[]>([]);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    if (!user) {
      return;
    }

    setLoading(true);
    try {
      const [scenarioItems, requestItems, published] = await Promise.all([
        browseCatalog({
          languageId: getLanguageId(),
          userId: user.id,
          isScenario: true,
          skip: 0,
          take: MAX_CATALOG_TAKE,
        }),
        listRequests(getLanguageId()),
        canFulfill
          ? browseCatalog({
              languageId: getLanguageId(),
              userId: user.id,
              skip: 0,
              take: MAX_CATALOG_TAKE,
            })
          : Promise.resolve([]),
      ]);
      setScenarios(scenarioItems);
      setFulfillLessons(published);
      setRequests(sortRequestsByUpvoteCount(requestItems));
    } catch (error) {
      const message =
        error instanceof CatalogError || error instanceof RequestError
          ? error.message
          : 'Could not load conversations.';
      toast.error(message);
    } finally {
      setLoading(false);
    }
  }, [browseCatalog, canFulfill, listRequests, user]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  const onSubmit = async () => {
    setBusy(true);
    try {
      const created = await submitRequest(getLanguageId(), title, description);
      setRequests(sortRequestsByUpvoteCount([created, ...requests.filter((item) => item.id !== created.id)]));
      setTitle('');
      setDescription('');
      toast.success('Request submitted.');
    } catch (error) {
      const message =
        error instanceof RequestValidationError || error instanceof RequestError
          ? error.message
          : 'Could not submit that request.';
      toast.error(message);
    } finally {
      setBusy(false);
    }
  };

  const onUpvote = async (requestId: string) => {
    setBusy(true);
    try {
      const result = await upvoteRequest(getLanguageId(), requestId);
      setRequests(
        sortRequestsByUpvoteCount(
          requests.map((item) => (item.id === result.request.id ? result.request : item)),
        ),
      );
      if (!result.applied) {
        toast.success('You already upvoted this request.');
      }
    } catch (error) {
      const message = error instanceof RequestError ? error.message : 'Could not upvote that request.';
      toast.error(message);
    } finally {
      setBusy(false);
    }
  };

  const onFulfill = async (requestId: string, lessonId: string) => {
    setBusy(true);
    try {
      const fulfilled = await fulfillRequest(getLanguageId(), requestId, lessonId);
      setRequests(requests.filter((item) => item.id !== fulfilled.id));
      toast.success('Request linked to a lesson.');
    } catch (error) {
      const message = error instanceof RequestError ? error.message : 'Could not link that lesson.';
      toast.error(message);
    } finally {
      setBusy(false);
    }
  };

  if (!user) {
    return <ActivityIndicator style={styles.center} />;
  }

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <ScrollView>
        <View style={styles.padded}>
          <ThemedText type="title">Conversations</ThemedText>
          <ThemedText>Real-life situations and community requests.</ThemedText>
        </View>
        {loading ? <ActivityIndicator /> : null}
        <View style={styles.padded}>
          <ThemedText type="subtitle">Scenarios</ThemedText>
          {!loading && scenarios.length === 0 ? (
            <ThemedText>No scenario lessons yet.</ThemedText>
          ) : null}
          {scenarios.map((lesson) => (
            <LessonCard
              key={lesson.id}
              lesson={lesson}
              onPress={(item) => router.push({ pathname: '/conversation', params: { id: item.id } })}
            />
          ))}
        </View>
        <RequestBoard
          requests={requests}
          title={title}
          description={description}
          onTitleChange={setTitle}
          onDescriptionChange={setDescription}
          onSubmit={() => void onSubmit()}
          onUpvote={(requestId) => void onUpvote(requestId)}
          onFulfill={canFulfill ? (requestId, lessonId) => void onFulfill(requestId, lessonId) : undefined}
          fulfillLessons={fulfillLessons}
          canFulfill={canFulfill}
          busy={busy}
        />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
  },
  center: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  padded: {
    paddingHorizontal: 24,
    paddingTop: 16,
    gap: 12,
    paddingBottom: 8,
  },
});
