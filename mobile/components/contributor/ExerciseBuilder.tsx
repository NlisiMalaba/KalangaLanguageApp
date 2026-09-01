import { Pressable, StyleSheet, TextInput, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { emptyExercise } from '@/domain/contributor/validateLessonDraft';
import type { DraftExercise } from '@/domain/contributor/types';
import { ExerciseType } from '@/domain/enums';

const TYPES = Object.values(ExerciseType);

export function ExerciseBuilder({
  exercises,
  onChange,
}: {
  exercises: DraftExercise[];
  onChange: (exercises: DraftExercise[]) => void;
}) {
  const update = (index: number, patch: Partial<DraftExercise>) => {
    onChange(exercises.map((exercise, current) => (current === index ? { ...exercise, ...patch } : exercise)));
  };

  return (
    <View style={styles.block}>
      <ThemedText type="subtitle">Exercises</ThemedText>
      {exercises.map((exercise, index) => (
        <View key={exercise.clientKey} style={styles.card}>
          <View style={styles.row}>
            {TYPES.map((type) => (
              <Pressable
                key={type}
                onPress={() => update(index, { exerciseType: type })}
                accessibilityRole="button"
                accessibilityLabel={`Exercise type ${type}`}>
                <ThemedText type={exercise.exerciseType === type ? 'defaultSemiBold' : 'link'}>{type}</ThemedText>
              </Pressable>
            ))}
          </View>
          <TextInput
            value={exercise.prompt}
            onChangeText={(prompt) => update(index, { prompt })}
            placeholder={exercise.exerciseType === ExerciseType.Flashcard ? 'Kalanga prompt' : 'Prompt'}
            accessibilityLabel={`Exercise ${index + 1} prompt`}
            style={styles.input}
          />
          {exercise.exerciseType === ExerciseType.Flashcard ? (
            <TextInput
              value={exercise.correctAnswer}
              onChangeText={(correctAnswer) => update(index, { correctAnswer })}
              placeholder="English translation"
              accessibilityLabel={`Exercise ${index + 1} answer`}
              style={styles.input}
            />
          ) : null}
          {(exercise.exerciseType === ExerciseType.MultipleChoice ||
            exercise.exerciseType === ExerciseType.Listening) &&
            exercise.options.map((option, optionIndex) => (
              <TextInput
                key={`${exercise.clientKey}-opt-${optionIndex}`}
                value={option}
                onChangeText={(value) => {
                  const options = [...exercise.options];
                  options[optionIndex] = value;
                  update(index, { options, correctIndex: exercise.correctIndex });
                }}
                placeholder={`Option ${optionIndex + 1}`}
                accessibilityLabel={`Exercise ${index + 1} option ${optionIndex + 1}`}
                style={styles.input}
              />
            ))}
          {exercise.exerciseType === ExerciseType.MultipleChoice ||
          exercise.exerciseType === ExerciseType.Listening ? (
            <TextInput
              value={String(exercise.correctIndex)}
              onChangeText={(value) => update(index, { correctIndex: Number(value) || 0 })}
              keyboardType="number-pad"
              accessibilityLabel={`Exercise ${index + 1} correct index`}
              style={styles.input}
            />
          ) : null}
          {exercise.exerciseType === ExerciseType.Listening ? (
            <TextInput
              value={exercise.recordingId ?? ''}
              onChangeText={(recordingId) => update(index, { recordingId: recordingId || null })}
              placeholder="Recording id"
              accessibilityLabel={`Exercise ${index + 1} recording id`}
              style={styles.input}
            />
          ) : null}
          {exercise.exerciseType === ExerciseType.SentenceBuilder ? (
            <TextInput
              value={exercise.tokens}
              onChangeText={(tokens) => update(index, { tokens })}
              placeholder="Tokens separated by spaces"
              accessibilityLabel={`Exercise ${index + 1} tokens`}
              style={styles.input}
            />
          ) : null}
        </View>
      ))}
      <Pressable
        onPress={() => onChange([...exercises, emptyExercise()])}
        accessibilityRole="button"
        accessibilityLabel="Add exercise">
        <ThemedText type="link">Add exercise</ThemedText>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  block: {
    gap: 12,
  },
  card: {
    gap: 8,
    padding: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#d0d5d8',
  },
  row: {
    flexDirection: 'row',
    flexWrap: 'wrap',
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
