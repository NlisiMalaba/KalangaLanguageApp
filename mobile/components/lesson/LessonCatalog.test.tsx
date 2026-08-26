import { render, fireEvent } from '@testing-library/react-native';

import { LessonCatalog } from '@/components/lesson/LessonCatalog';
import { LessonDownloadStatus, type CatalogLessonItem } from '@/domain/catalog/types';
import { Level } from '@/domain/enums';

const lesson: CatalogLessonItem = {
  id: 'lesson-1',
  languageId: 'lang-1',
  title: 'Greetings',
  level: Level.Beginner,
  category: 'Everyday',
  isScenario: false,
  scenarioContext: null,
  xpReward: 10,
  updatedAt: '2026-08-23T12:00:00.000Z',
  isCompleted: true,
  downloadStatus: LessonDownloadStatus.Downloaded,
};

describe('LessonCatalog', () => {
  it('shows completion and download status and opens a lesson', () => {
    const onOpenLesson = jest.fn();
    const { getByText, getByLabelText } = render(
      <LessonCatalog
        selectedLevel={Level.Beginner}
        selectedCategory={null}
        categories={['Everyday']}
        lessons={[lesson]}
        loading={false}
        error={null}
        onSelectLevel={jest.fn()}
        onSelectCategory={jest.fn()}
        onOpenLesson={onOpenLesson}
      />,
    );

    expect(getByText('Greetings')).toBeTruthy();
    expect(getByText('Completed')).toBeTruthy();
    expect(getByText('Downloaded')).toBeTruthy();

    fireEvent.press(getByLabelText(/Greetings/));
    expect(onOpenLesson).toHaveBeenCalledWith(lesson);
  });
});
