import { useEffect, useRef, useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';
import { toast } from 'sonner-native';

import { AudioPrompt } from '@/components/lesson/AudioPrompt';
import type { AudioPlayerFactory } from '@/components/lesson/AudioPrompt';
import { AudioWaveform } from '@/components/lesson/AudioWaveform';
import { FeedbackView } from '@/components/lesson/FeedbackView';
import { MicrophonePermissionModal } from '@/components/pronunciation/MicrophonePermissionModal';
import { ThemedText } from '@/components/themed-text';
import { Colors } from '@/constants/theme';
import type { AudioRef, LessonPhraseDetail } from '@/domain/catalog/types';
import type { EntityId } from '@/domain/entities';
import { MicrophonePermissionDeniedError, PronunciationError } from '@/domain/pronunciation/errors';
import {
  finishPronunciationPractice,
  referenceSampleFromAudio,
} from '@/domain/pronunciation/finishPronunciationPractice';
import type { PronunciationRecorder } from '@/domain/pronunciation/pronunciationRecorder';
import type { PronunciationScore, PronunciationTransmitter } from '@/domain/pronunciation/types';
import { useColorScheme } from '@/hooks/use-color-scheme';
import { addListeningMs, addSpeakingMs } from '@/lib/speakingListeningStats';

const skippedTransmitter: PronunciationTransmitter = {
  async upload() {
    return;
  },
};

export type PronunciationRecorderFactory = () => PronunciationRecorder;

export function PronunciationPractice({
  languageId,
  phrase,
  recordings,
  createRecorder,
  createPlayer,
  consentGranted = false,
  transmitter = skippedTransmitter,
  userId,
}: {
  languageId: EntityId;
  phrase: LessonPhraseDetail;
  recordings: readonly AudioRef[];
  createRecorder: PronunciationRecorderFactory;
  createPlayer?: AudioPlayerFactory;
  consentGranted?: boolean;
  transmitter?: PronunciationTransmitter;
  userId?: EntityId;
}) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const recorderRef = useRef<PronunciationRecorder | null>(null);
  const createRecorderRef = useRef(createRecorder);
  createRecorderRef.current = createRecorder;
  const recordingRef = useRef(false);
  const startingRef = useRef(false);
  const stoppingRef = useRef(false);
  const [recording, setRecording] = useState(false);
  const [busy, setBusy] = useState(false);
  const [permissionDenied, setPermissionDenied] = useState(false);
  const [feedback, setFeedback] = useState<PronunciationScore | null>(null);

  useEffect(() => {
    recorderRef.current = createRecorderRef.current();
    recordingRef.current = false;
    startingRef.current = false;
    stoppingRef.current = false;
    setRecording(false);
    setFeedback(null);
    setPermissionDenied(false);

    return () => {
      recorderRef.current = null;
    };
  }, [phrase.id]);

  const selectedReference = recordings[0];

  const onToggleRecord = () => {
    void (async () => {
      const recorder = recorderRef.current;
      if (!recorder) {
        return;
      }

      if (!recordingRef.current) {
        if (startingRef.current) {
          return;
        }

        startingRef.current = true;
        setBusy(true);
        try {
          await recorder.start(languageId);
          recordingRef.current = true;
          setRecording(true);
          setFeedback(null);
        } catch (error) {
          recordingRef.current = false;
          setRecording(false);
          if (error instanceof MicrophonePermissionDeniedError) {
            setPermissionDenied(true);
            return;
          }

          const message =
            error instanceof PronunciationError ? error.message : 'Could not record pronunciation.';
          toast.error(message);
        } finally {
          startingRef.current = false;
          setBusy(false);
        }

        return;
      }

      if (stoppingRef.current) {
        return;
      }

      stoppingRef.current = true;
      setBusy(true);
      try {
        const learner = await recorder.stop();
        recordingRef.current = false;
        setRecording(false);
        if (userId) {
          void addSpeakingMs(userId, learner.durationMs);
        }
        const result = await finishPronunciationPractice({
          learner,
          reference: referenceSampleFromAudio(selectedReference),
          consentGranted,
          transmitter,
        });
        setFeedback(result.score);
      } catch (error) {
        recordingRef.current = false;
        setRecording(false);
        if (error instanceof MicrophonePermissionDeniedError) {
          setPermissionDenied(true);
          return;
        }

        const message = error instanceof PronunciationError ? error.message : 'Could not record pronunciation.';
        toast.error(message);
      } finally {
        stoppingRef.current = false;
        setBusy(false);
      }
    })();
  };

  return (
    <View style={styles.container}>
      <ThemedText type="title">{phrase.kalangaText}</ThemedText>
      <ThemedText style={{ color: colors.icon }}>{phrase.englishTranslation}</ThemedText>

      <ThemedText type="subtitle">Reference</ThemedText>
      <AudioPrompt
        languageId={languageId}
        recordings={recordings}
        ttsText={phrase.kalangaText}
        createPlayer={createPlayer}
        onHeardMs={userId ? (durationMs) => void addListeningMs(userId, durationMs) : undefined}
      />

      <View style={styles.recordRow}>
        <Pressable
          onPress={onToggleRecord}
          disabled={busy}
          accessibilityRole="button"
          accessibilityLabel={recording ? 'Stop recording' : 'Record pronunciation'}
          accessibilityState={{ busy, selected: recording }}
          style={[styles.record, { backgroundColor: recording ? '#c0392b' : colors.tint }]}>
          <ThemedText style={styles.recordLabel}>{recording ? 'Stop' : 'Record'}</ThemedText>
        </Pressable>
        <AudioWaveform
          playing={recording}
          accessibilityLabel={recording ? 'Recording in progress' : 'Recorder idle'}
        />
      </View>

      {feedback ? <FeedbackView score={feedback} /> : null}

      <MicrophonePermissionModal
        visible={permissionDenied}
        onOpenSettings={() => {
          void recorderRef.current?.openSettings();
          setPermissionDenied(false);
        }}
        onDismiss={() => setPermissionDenied(false)}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
  },
  recordRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    marginTop: 8,
  },
  record: {
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 10,
  },
  recordLabel: {
    color: '#fff',
    fontWeight: '600',
  },
});
