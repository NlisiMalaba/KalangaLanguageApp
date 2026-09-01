import { Pressable, StyleSheet, TextInput, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import type { CatalogLessonItem } from '@/domain/catalog/types';
import type { CommunityRequest } from '@/domain/requests';
import { RequestStatus } from '@/domain/enums';

export function RequestBoard({
  requests,
  title,
  description,
  onTitleChange,
  onDescriptionChange,
  onSubmit,
  onUpvote,
  onFulfill,
  fulfillLessons = [],
  canFulfill = false,
  busy = false,
}: {
  requests: CommunityRequest[];
  title: string;
  description: string;
  onTitleChange: (value: string) => void;
  onDescriptionChange: (value: string) => void;
  onSubmit: () => void;
  onUpvote: (requestId: string) => void;
  onFulfill?: (requestId: string, lessonId: string) => void;
  fulfillLessons?: CatalogLessonItem[];
  canFulfill?: boolean;
  busy?: boolean;
}) {
  return (
    <View style={styles.block}>
      <ThemedText type="subtitle">Community requests</ThemedText>
      <ThemedText>Ask for a situation or upvote what others need.</ThemedText>
      <TextInput
        value={title}
        onChangeText={onTitleChange}
        placeholder="Request title"
        accessibilityLabel="Request title"
        style={styles.input}
      />
      <TextInput
        value={description}
        onChangeText={onDescriptionChange}
        placeholder="What phrases or situation do you need?"
        accessibilityLabel="Request description"
        multiline
        style={[styles.input, styles.multiline]}
      />
      <Pressable
        onPress={onSubmit}
        disabled={busy}
        accessibilityRole="button"
        accessibilityLabel="Submit request">
        <ThemedText type="link">Submit request</ThemedText>
      </Pressable>

      {requests.length === 0 ? <ThemedText>No open requests yet.</ThemedText> : null}
      {requests.map((request) => (
        <View key={request.id} style={styles.card}>
          <ThemedText type="defaultSemiBold">{request.title}</ThemedText>
          <ThemedText>{request.description}</ThemedText>
          <ThemedText accessibilityLabel={`${request.upvoteCount} upvotes`}>
            {request.upvoteCount} upvote{request.upvoteCount === 1 ? '' : 's'}
          </ThemedText>
          {request.status === RequestStatus.Open ? (
            <Pressable
              onPress={() => onUpvote(request.id)}
              disabled={busy}
              accessibilityRole="button"
              accessibilityLabel={`Upvote ${request.title}`}>
              <ThemedText type="link">Upvote</ThemedText>
            </Pressable>
          ) : null}
          {canFulfill && request.status === RequestStatus.Open && onFulfill ? (
            <View style={styles.fulfill}>
              <ThemedText>Link a published lesson</ThemedText>
              {fulfillLessons.length === 0 ? (
                <ThemedText>Publish a lesson first, then link it here.</ThemedText>
              ) : null}
              {fulfillLessons.map((lesson) => (
                <Pressable
                  key={lesson.id}
                  onPress={() => onFulfill(request.id, lesson.id)}
                  disabled={busy}
                  accessibilityRole="button"
                  accessibilityLabel={`Fulfill ${request.title} with ${lesson.title}`}>
                  <ThemedText type="link">{lesson.title}</ThemedText>
                </Pressable>
              ))}
            </View>
          ) : null}
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  block: {
    gap: 12,
    paddingHorizontal: 24,
    paddingBottom: 48,
  },
  card: {
    gap: 6,
    padding: 12,
    borderWidth: 1,
    borderColor: '#d0d5d8',
    borderRadius: 12,
  },
  fulfill: {
    gap: 6,
    marginTop: 8,
  },
  input: {
    borderWidth: 1,
    borderColor: '#d0d5d8',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontSize: 16,
  },
  multiline: {
    minHeight: 80,
    textAlignVertical: 'top',
  },
});
