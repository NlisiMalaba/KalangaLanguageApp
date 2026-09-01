import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';
import { toast } from 'sonner-native';

import { LessonBuilder } from '@/components/contributor/LessonBuilder';
import { ThemedText } from '@/components/themed-text';
import { getLanguageId } from '@/constants/config';
import { useAuth } from '@/ctx/AuthContext';
import {
  ContributorError,
  ContributorValidationError,
  createSaveContributorLessonUseCase,
  createSubmitContributorLessonUseCase,
  emptyLessonDraft,
  type ContributorLessonApi,
  type LessonDraft,
} from '@/domain/contributor';
import { LessonStatus, Role } from '@/domain/enums';
import { createHttpContributorApi } from '@/lib/contributor/httpContributorApi';
import { pickAudioFile, readAudioDurationMs } from '@/lib/contributor/pickAudioFile';

const defaultApi = createHttpContributorApi();
const defaultSave = createSaveContributorLessonUseCase(defaultApi);
const defaultSubmit = createSubmitContributorLessonUseCase(defaultApi);

export default function ContributorLessonScreen({
  api = defaultApi,
  saveLesson = defaultSave,
  submitLesson = defaultSubmit,
}: {
  api?: ContributorLessonApi;
  saveLesson?: (draft: LessonDraft, languageId: string) => Promise<{ lessonId: string }>;
  submitLesson?: (draft: LessonDraft, languageId: string) => Promise<LessonStatus>;
}) {
  const { id } = useLocalSearchParams<{ id: string }>();
  const { user } = useAuth();
  const [draft, setDraft] = useState<LessonDraft>(emptyLessonDraft());
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(id != null && id !== 'new');

  const canAuthor = user?.role === Role.Contributor || user?.role === Role.Admin;

  const load = useCallback(async () => {
    if (!id || id === 'new') {
      setDraft(emptyLessonDraft());
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      setDraft(await api.getDraft(getLanguageId(), id));
    } catch (error) {
      const message = error instanceof ContributorError ? error.message : 'Could not load that draft.';
      toast.error(message);
    } finally {
      setLoading(false);
    }
  }, [api, id]);

  useEffect(() => {
    void load();
  }, [load]);

  const onSave = async () => {
    setBusy(true);
    try {
      const saved = await saveLesson(draft, getLanguageId());
      const reloaded = await api.getDraft(getLanguageId(), saved.lessonId);
      setDraft(reloaded);
      toast.success('Draft saved.');
      if (id === 'new' || !id) {
        router.replace(`/contributor/${saved.lessonId}`);
      }
    } catch (error) {
      const message =
        error instanceof ContributorValidationError || error instanceof ContributorError
          ? error.message
          : 'Could not save the draft.';
      toast.error(message);
    } finally {
      setBusy(false);
    }
  };

  const onSubmit = async () => {
    setBusy(true);
    try {
      const saved = await saveLesson(draft, getLanguageId());
      const reloaded = await api.getDraft(getLanguageId(), saved.lessonId);
      const status = await submitLesson(reloaded, getLanguageId());
      setDraft({ ...reloaded, id: saved.lessonId, status });
      toast.success('Submitted for review.');
    } catch (error) {
      const message =
        error instanceof ContributorValidationError || error instanceof ContributorError
          ? error.message
          : 'Could not submit the lesson.';
      toast.error(message);
    } finally {
      setBusy(false);
    }
  };

  const onUploadAudio = async (phraseIndex: number) => {
    const phrase = draft.phrases[phraseIndex];
    if (!phrase?.id) {
      toast.error('Save the draft before uploading audio.');
      return;
    }

    try {
      const file = await pickAudioFile();
      if (!file) {
        return;
      }

      const durationMs = await readAudioDurationMs(file.uri);
      const uploaded = await api.uploadAudio({ file, durationMs, phraseId: phrase.id });
      const phrases = draft.phrases.map((item, index) =>
        index === phraseIndex
          ? {
              ...item,
              audio: [
                ...item.audio,
                {
                  id: uploaded.audioRecordingId,
                  cdnUrl: uploaded.cdnUrl,
                  fileFormat: 'Mp3',
                  speakerGender: 'Unspecified',
                  dialectLabel: null,
                  durationMs: uploaded.durationMs,
                },
              ],
            }
          : item,
      );
      setDraft({ ...draft, phrases });
      toast.success('Audio uploaded.');
    } catch (error) {
      const message =
        error instanceof ContributorValidationError || error instanceof ContributorError
          ? error.message
          : 'Could not upload audio.';
      toast.error(message);
    }
  };

  if (!user || !canAuthor) {
    return (
      <SafeAreaView style={styles.safe} edges={['top']}>
        <ThemedText>Contributor access is required to create lessons.</ThemedText>
      </SafeAreaView>
    );
  }

  if (loading) {
    return <ActivityIndicator style={styles.center} />;
  }

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <ScrollView contentContainerStyle={styles.padded}>
        <LessonBuilder
          draft={draft}
          onChange={setDraft}
          onSave={() => void onSave()}
          onSubmit={() => void onSubmit()}
          onUploadAudio={(index) => void onUploadAudio(index)}
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
  padded: {
    padding: 24,
  },
  center: {
    flex: 1,
    justifyContent: 'center',
  },
});
