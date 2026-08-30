import { useEffect, useMemo, useRef, useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';
import { toast } from 'sonner-native';

import { AudioWaveform } from '@/components/lesson/AudioWaveform';
import { ThemedText } from '@/components/themed-text';
import { PlaybackRate } from '@/domain/audio/types';
import { AudioError } from '@/domain/audio/errors';
import type { AudioPlayer } from '@/domain/audio/audioPlayer';
import type { AudioRef } from '@/domain/catalog/types';
import type { EntityId } from '@/domain/entities';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';
import { clearActivePhrasePlayer, stopOtherPhrasePlayers } from '@/lib/audio/activePhrasePlayer';
import { createDefaultAudioPlayer } from '@/lib/audio/createAudioPlayer';

export type AudioPlayerFactory = (session: {
  languageId: EntityId;
  recordings: readonly AudioRef[];
  ttsText?: string | null;
}) => AudioPlayer;

const defaultCreatePlayer = createDefaultAudioPlayer();

export type AudioPromptProps = {
  languageId: EntityId;
  recordings: readonly AudioRef[];
  ttsText: string;
  createPlayer?: AudioPlayerFactory;
};

function recordingLabel(recording: AudioRef, index: number): string {
  const parts = [
    recording.dialectLabel,
    recording.speakerGender !== 'Unspecified' ? recording.speakerGender : null,
  ].filter((part): part is string => Boolean(part));

  return parts.length > 0 ? parts.join(' · ') : `Recording ${index + 1}`;
}

export function AudioPrompt({
  languageId,
  recordings,
  ttsText,
  createPlayer = defaultCreatePlayer,
}: AudioPromptProps) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const playerRef = useRef<AudioPlayer | null>(null);
  const playingTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [hasPlayed, setHasPlayed] = useState(false);
  const [selectedId, setSelectedId] = useState<EntityId | null>(recordings[0]?.id ?? null);
  const [rate, setRate] = useState<PlaybackRate>(PlaybackRate.Normal);
  const [playing, setPlaying] = useState(false);

  const recordingKey = useMemo(() => recordings.map((item) => item.id).join(','), [recordings]);

  useEffect(() => {
    const player = createPlayer({ languageId, recordings, ttsText });
    playerRef.current = player;
    setHasPlayed(false);
    setSelectedId(player.selectedRecordingId());
    setRate(player.playbackRate());
    setPlaying(false);

    return () => {
      if (playingTimeout.current) {
        clearTimeout(playingTimeout.current);
      }
      clearActivePhrasePlayer(player);
      void player.dispose();
      playerRef.current = null;
    };
  }, [createPlayer, languageId, recordingKey, ttsText, recordings]);

  const markPlaying = (nextRate: PlaybackRate) => {
    if (playingTimeout.current) {
      clearTimeout(playingTimeout.current);
    }

    const selected = recordings.find((item) => item.id === selectedId);
    const durationMs = Math.max(400, Math.round((selected?.durationMs ?? 1500) / nextRate));
    setPlaying(true);
    playingTimeout.current = setTimeout(() => setPlaying(false), durationMs);
  };

  const run = async (action: (player: AudioPlayer) => Promise<unknown>, nextRate = rate) => {
    const player = playerRef.current;
    if (!player) {
      return;
    }

    try {
      await stopOtherPhrasePlayers(player);
      await action(player);
      markPlaying(nextRate);
    } catch (error) {
      setPlaying(false);
      const message = error instanceof AudioError ? error.message : 'Could not play audio.';
      toast.error(message);
    }
  };

  const onPlay = () => {
    void run((player) => {
      if (hasPlayed) {
        return player.replay();
      }

      setHasPlayed(true);
      return player.play();
    });
  };

  const onToggleRate = () => {
    const next = rate === PlaybackRate.Normal ? PlaybackRate.Slow : PlaybackRate.Normal;
    void run(async (player) => {
      await player.setRate(next);
      setRate(next);
    }, next);
  };

  const onSelectRecording = (recordingId: EntityId) => {
    const player = playerRef.current;
    if (!player) {
      return;
    }

    try {
      player.selectRecording(recordingId);
      setSelectedId(recordingId);
      setHasPlayed(false);
    } catch (error) {
      const message = error instanceof AudioError ? error.message : 'Could not select that recording.';
      toast.error(message);
    }
  };

  const rateLabel = rate === PlaybackRate.Slow ? '0.75x' : '1x';

  return (
    <View style={styles.container}>
      <View style={styles.controls}>
        <Pressable
          onPress={onPlay}
          accessibilityRole="button"
          accessibilityLabel={hasPlayed ? 'Replay audio' : 'Play audio'}
          style={[styles.play, { backgroundColor: colors.tint }]}>
          <ThemedText style={styles.playLabel}>{playing ? 'Playing' : hasPlayed ? 'Replay' : 'Play'}</ThemedText>
        </Pressable>
        <Pressable
          onPress={onToggleRate}
          accessibilityRole="button"
          accessibilityLabel={`Playback speed ${rateLabel}`}
          accessibilityState={{ selected: rate === PlaybackRate.Slow }}
          style={[styles.chip, { borderColor: colors.icon }]}>
          <ThemedText>{rateLabel}</ThemedText>
        </Pressable>
        <AudioWaveform playing={playing} />
      </View>

      {recordings.length > 1 ? (
        <View style={styles.recordings} accessibilityRole="tablist">
          {recordings.map((recording, index) => {
            const selected = recording.id === selectedId;
            return (
              <Pressable
                key={recording.id}
                onPress={() => onSelectRecording(recording.id)}
                accessibilityRole="tab"
                accessibilityState={{ selected }}
                accessibilityLabel={recordingLabel(recording, index)}
                style={[
                  styles.chip,
                  { borderColor: selected ? colors.tint : colors.icon },
                  selected && { backgroundColor: colors.tint },
                ]}>
                <ThemedText style={selected ? styles.selectedChipText : undefined}>
                  {recordingLabel(recording, index)}
                </ThemedText>
              </Pressable>
            );
          })}
        </View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 8,
    marginTop: 8,
  },
  controls: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  play: {
    borderRadius: 20,
    paddingHorizontal: 14,
    paddingVertical: 8,
  },
  playLabel: {
    color: '#fff',
    fontWeight: '600',
  },
  chip: {
    borderWidth: 1,
    borderRadius: 16,
    paddingHorizontal: 10,
    paddingVertical: 6,
  },
  selectedChipText: {
    color: '#fff',
    fontWeight: '600',
  },
  recordings: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
});
