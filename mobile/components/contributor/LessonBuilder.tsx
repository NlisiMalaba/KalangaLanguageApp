import { Pressable, StyleSheet, Switch, TextInput, View } from 'react-native';

import { ExerciseBuilder } from '@/components/contributor/ExerciseBuilder';
import { ThemedText } from '@/components/themed-text';
import { CONTRIBUTOR_CATEGORIES } from '@/constants/contributor';
import { emptyPhrase } from '@/domain/contributor/validateLessonDraft';
import type { DraftPhrase, LessonDraft } from '@/domain/contributor/types';
import { Level } from '@/domain/enums';

const LEVELS = Object.values(Level);

export function LessonBuilder({
  draft,
  onChange,
  onSave,
  onSubmit,
  onUploadAudio,
  busy = false,
}: {
  draft: LessonDraft;
  onChange: (draft: LessonDraft) => void;
  onSave: () => void;
  onSubmit: () => void;
  onUploadAudio?: (phraseIndex: number) => void;
  busy?: boolean;
}) {
  const updatePhrase = (index: number, patch: Partial<DraftPhrase>) => {
    onChange({
      ...draft,
      phrases: draft.phrases.map((phrase, current) => (current === index ? { ...phrase, ...patch } : phrase)),
    });
  };

  return (
    <View style={styles.block}>
      <ThemedText type="title">Lesson builder</ThemedText>
      {draft.status ? <ThemedText>Status: {draft.status}</ThemedText> : null}
      <TextInput
        value={draft.title}
        onChangeText={(title) => onChange({ ...draft, title })}
        placeholder="Title"
        accessibilityLabel="Lesson title"
        style={styles.input}
      />
      <View style={styles.row}>
        {LEVELS.map((level) => (
          <Pressable
            key={level}
            onPress={() => onChange({ ...draft, level })}
            accessibilityRole="button"
            accessibilityLabel={`Level ${level}`}>
            <ThemedText type={draft.level === level ? 'defaultSemiBold' : 'link'}>{level}</ThemedText>
          </Pressable>
        ))}
      </View>
      <View style={styles.row}>
        {CONTRIBUTOR_CATEGORIES.map((category) => (
          <Pressable
            key={category}
            onPress={() => onChange({ ...draft, category })}
            accessibilityRole="button"
            accessibilityLabel={`Category ${category}`}>
            <ThemedText type={draft.category === category ? 'defaultSemiBold' : 'link'}>{category}</ThemedText>
          </Pressable>
        ))}
      </View>
      <View style={styles.row}>
        <ThemedText>Scenario</ThemedText>
        <Switch
          value={draft.isScenario}
          onValueChange={(isScenario) => onChange({ ...draft, isScenario })}
          accessibilityLabel="Scenario lesson"
        />
      </View>
      {draft.isScenario ? (
        <TextInput
          value={draft.scenarioContext}
          onChangeText={(scenarioContext) => onChange({ ...draft, scenarioContext })}
          placeholder="Situation context"
          accessibilityLabel="Scenario context"
          style={styles.input}
        />
      ) : null}
      <ThemedText type="subtitle">Phrases</ThemedText>
      {draft.phrases.map((phrase, index) => (
        <View key={phrase.clientKey} style={styles.card}>
          <TextInput
            value={phrase.kalangaText}
            onChangeText={(kalangaText) => updatePhrase(index, { kalangaText })}
            placeholder="Kalanga text"
            accessibilityLabel={`Phrase ${index + 1} Kalanga`}
            style={styles.input}
          />
          <TextInput
            value={phrase.englishTranslation}
            onChangeText={(englishTranslation) => updatePhrase(index, { englishTranslation })}
            placeholder="English translation"
            accessibilityLabel={`Phrase ${index + 1} English`}
            style={styles.input}
          />
          {phrase.variations.map((variation, variationIndex) => (
            <View key={variation.clientKey} style={styles.variation}>
              <TextInput
                value={variation.kalangaText}
                onChangeText={(kalangaText) => {
                  const variations = phrase.variations.map((item, current) =>
                    current === variationIndex ? { ...item, kalangaText } : item,
                  );
                  updatePhrase(index, { variations });
                }}
                placeholder="Variation Kalanga"
                accessibilityLabel={`Phrase ${index + 1} variation ${variationIndex + 1} Kalanga`}
                style={styles.input}
              />
              <TextInput
                value={variation.registerLabel}
                onChangeText={(registerLabel) => {
                  const variations = phrase.variations.map((item, current) =>
                    current === variationIndex ? { ...item, registerLabel } : item,
                  );
                  updatePhrase(index, { variations });
                }}
                placeholder="Register label"
                accessibilityLabel={`Phrase ${index + 1} variation ${variationIndex + 1} register`}
                style={styles.input}
              />
            </View>
          ))}
          <Pressable
            onPress={() =>
              updatePhrase(index, {
                variations: [
                  ...phrase.variations,
                  {
                    clientKey: `${phrase.clientKey}-var-${phrase.variations.length}`,
                    id: null,
                    kalangaText: '',
                    registerLabel: '',
                  },
                ],
              })
            }
            accessibilityRole="button"
            accessibilityLabel={`Add variation to phrase ${index + 1}`}>
            <ThemedText type="link">Add variation</ThemedText>
          </Pressable>
          {onUploadAudio ? (
            <Pressable
              onPress={() => onUploadAudio(index)}
              accessibilityRole="button"
              accessibilityLabel={`Upload audio for phrase ${index + 1}`}>
              <ThemedText type="link">
                {phrase.audio.length > 0 ? `Audio attached (${phrase.audio.length})` : 'Upload audio'}
              </ThemedText>
            </Pressable>
          ) : null}
        </View>
      ))}
      <Pressable
        onPress={() => onChange({ ...draft, phrases: [...draft.phrases, emptyPhrase()] })}
        accessibilityRole="button"
        accessibilityLabel="Add phrase">
        <ThemedText type="link">Add phrase</ThemedText>
      </Pressable>
      <ExerciseBuilder
        exercises={draft.exercises}
        onChange={(exercises) => onChange({ ...draft, exercises })}
      />
      <Pressable
        onPress={onSave}
        disabled={busy}
        accessibilityRole="button"
        accessibilityLabel="Save draft">
        <ThemedText type="link">Save draft</ThemedText>
      </Pressable>
      <Pressable
        onPress={onSubmit}
        disabled={busy}
        accessibilityRole="button"
        accessibilityLabel="Submit for review">
        <ThemedText type="link">Submit for review</ThemedText>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  block: {
    gap: 12,
    paddingBottom: 48,
  },
  row: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    alignItems: 'center',
  },
  card: {
    gap: 8,
    padding: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#d0d5d8',
  },
  variation: {
    gap: 8,
  },
  input: {
    borderWidth: 1,
    borderColor: '#d0d5d8',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontSize: 16,
  },
});
