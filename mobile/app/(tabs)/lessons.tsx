import { useCallback, useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet } from 'react-native';
import { router, useFocusEffect } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';
import { toast } from 'sonner-native';

import { LessonCatalog } from '@/components/lesson/LessonCatalog';
import { ThemedText } from '@/components/themed-text';
import { MAX_CATALOG_TAKE } from '@/constants/catalog';
import { getLanguageId } from '@/constants/config';
import { useAuth } from '@/ctx/AuthContext';
import type { CatalogLessonItem } from '@/domain/catalog/types';
import { Level } from '@/domain/enums';
import { createDefaultBrowseLessonCatalogUseCase } from '@/lib/catalog/createBrowseLessonCatalogUseCase';
import { CatalogError } from '@/domain/catalog/errors';

const browseCatalog = createDefaultBrowseLessonCatalogUseCase();

export default function LessonsScreen() {
  const { user, profile } = useAuth();
  const startingLevel = profile?.startingLevel ?? Level.Beginner;
  const [selectedLevel, setSelectedLevel] = useState<Level>(startingLevel);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [lessons, setLessons] = useState<CatalogLessonItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setSelectedLevel(startingLevel);
  }, [startingLevel]);

  const load = useCallback(async () => {
    if (!user) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const items = await browseCatalog({
        languageId: getLanguageId(),
        userId: user.id,
        level: selectedLevel,
        skip: 0,
        take: MAX_CATALOG_TAKE,
      });
      setLessons(items);
    } catch (caught) {
      const message = caught instanceof CatalogError ? caught.message : 'Could not load lessons.';
      setError(message);
      toast.error(message);
    } finally {
      setLoading(false);
    }
  }, [user, selectedLevel]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  const categories = useMemo(() => {
    const unique = new Set(lessons.map((lesson) => lesson.category));
    return [...unique].sort((a, b) => a.localeCompare(b));
  }, [lessons]);

  const visibleLessons = useMemo(
    () => (selectedCategory ? lessons.filter((lesson) => lesson.category === selectedCategory) : lessons),
    [lessons, selectedCategory],
  );

  const onSelectLevel = (level: Level) => {
    setSelectedLevel(level);
    setSelectedCategory(null);
  };

  if (!user) {
    return <ActivityIndicator style={styles.center} />;
  }

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <ScrollView>
        <Pressable
          onPress={() => router.push({ pathname: '/practise', params: { mode: 'review' } })}
          accessibilityRole="button"
          accessibilityLabel="Daily review"
          style={styles.review}>
          <ThemedText type="link">Daily review</ThemedText>
        </Pressable>
        <LessonCatalog
          selectedLevel={selectedLevel}
          selectedCategory={selectedCategory}
          categories={categories}
          lessons={visibleLessons}
          loading={loading}
          error={error}
          onSelectLevel={onSelectLevel}
          onSelectCategory={setSelectedCategory}
          onOpenLesson={(lesson) => router.push(`/lesson/${lesson.id}`)}
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
  review: {
    paddingHorizontal: 24,
    paddingTop: 16,
  },
});
