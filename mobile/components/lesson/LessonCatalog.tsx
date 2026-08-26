import { Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { LessonCard } from '@/components/lesson/LessonCard';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import type { CatalogLessonItem } from '@/domain/catalog/types';
import { Level } from '@/domain/enums';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

const LEVELS = [Level.Beginner, Level.Intermediate, Level.Advanced] as const;
const ALL_CATEGORIES = 'All';

export type LessonCatalogProps = {
  selectedLevel: Level;
  selectedCategory: string | null;
  categories: string[];
  lessons: CatalogLessonItem[];
  loading: boolean;
  error: string | null;
  onSelectLevel: (level: Level) => void;
  onSelectCategory: (category: string | null) => void;
  onOpenLesson: (lesson: CatalogLessonItem) => void;
};

export function LessonCatalog({
  selectedLevel,
  selectedCategory,
  categories,
  lessons,
  loading,
  error,
  onSelectLevel,
  onSelectCategory,
  onOpenLesson,
}: LessonCatalogProps) {
  const scheme = useColorScheme() ?? 'light';
  const colors = Colors[scheme];
  const categoryTabs = [ALL_CATEGORIES, ...categories];

  return (
    <ThemedView style={styles.container}>
      <ThemedText type="title">Lessons</ThemedText>
      <ThemedText>Filter by level and category. Completion and download status are shown on each card.</ThemedText>

      <ThemedText type="subtitle">Level</ThemedText>
      <View style={styles.row} accessibilityRole="tablist">
        {LEVELS.map((level) => (
          <Chip
            key={level}
            label={level}
            selected={selectedLevel === level}
            onPress={() => onSelectLevel(level)}
            tint={colors.tint}
          />
        ))}
      </View>

      <ThemedText type="subtitle">Category</ThemedText>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.row}>
        {categoryTabs.map((category) => {
          const value = category === ALL_CATEGORIES ? null : category;
          return (
            <Chip
              key={category}
              label={category}
              selected={selectedCategory === value}
              onPress={() => onSelectCategory(value)}
              tint={colors.tint}
            />
          );
        })}
      </ScrollView>

      {loading ? <ThemedText>Loading catalog…</ThemedText> : null}
      {error ? <ThemedText>{error}</ThemedText> : null}
      {!loading && !error && lessons.length === 0 ? (
        <ThemedText>No lessons at this level yet. Download a content pack or come back online.</ThemedText>
      ) : null}

      <View style={styles.list}>
        {lessons.map((lesson) => (
          <LessonCard key={lesson.id} lesson={lesson} onPress={onOpenLesson} />
        ))}
      </View>
    </ThemedView>
  );
}

function Chip({
  label,
  selected,
  onPress,
  tint,
}: {
  label: string;
  selected: boolean;
  onPress: () => void;
  tint: string;
}) {
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="tab"
      accessibilityState={{ selected }}
      accessibilityLabel={label}
      style={[styles.chip, selected && { backgroundColor: tint, borderColor: tint }]}>
      <ThemedText style={selected ? styles.chipSelectedText : undefined}>{label}</ThemedText>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
    gap: 12,
    paddingTop: 16,
  },
  row: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    alignItems: 'center',
  },
  chip: {
    borderWidth: 1,
    borderColor: '#687076',
    borderRadius: 20,
    paddingHorizontal: 14,
    paddingVertical: 8,
  },
  chipSelectedText: {
    color: '#fff',
    fontWeight: '600',
  },
  list: {
    gap: 12,
    paddingBottom: 32,
  },
});
