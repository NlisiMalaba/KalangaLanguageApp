import { encodeExercisePrompt } from '@/domain/contributor/encodeExercisePrompt';
import { emptyExercise, emptyLessonDraft } from '@/domain/contributor/validateLessonDraft';
import { createSaveContributorLessonUseCase } from '@/domain/contributor/saveContributorLesson';
import type { ContributorLessonApi } from '@/domain/contributor/types';
import { ExerciseType, LessonStatus } from '@/domain/enums';

describe('encodeExercisePrompt', () => {
  it('encodes flashcards, multiple choice, and sentence builder fields', () => {
    const flashcard = encodeExercisePrompt({
      ...emptyExercise(ExerciseType.Flashcard),
      prompt: 'Mhoro',
      correctAnswer: 'Hello',
    });
    expect(JSON.parse(flashcard.promptData)).toMatchObject({ kalanga_text: 'Mhoro' });
    expect(flashcard.correctAnswer).toBe('Hello');

    const choice = encodeExercisePrompt({
      ...emptyExercise(ExerciseType.MultipleChoice),
      prompt: 'Choose',
      options: ['A', 'B', 'C', 'D'],
      correctIndex: 2,
    });
    expect(JSON.parse(choice.promptData)).toMatchObject({ options: ['A', 'B', 'C', 'D'], correct_index: 2 });
    expect(choice.correctAnswer).toBe('C');

    const sentence = encodeExercisePrompt({
      ...emptyExercise(ExerciseType.SentenceBuilder),
      tokens: 'Ndino da chikafu',
    });
    expect(JSON.parse(sentence.promptData).tokens).toEqual(['Ndino', 'da', 'chikafu']);
    expect(sentence.correctAnswer).toBe('Ndino da chikafu');
  });
});

describe('createSaveContributorLessonUseCase', () => {
  it('creates then saves a new draft', async () => {
    const api: ContributorLessonApi = {
      createLesson: jest.fn(async () => ({ lessonId: 'lesson-1', status: LessonStatus.Draft })),
      saveDraft: jest.fn(async () => ({ lessonId: 'lesson-1', status: LessonStatus.Draft })),
      submitForReview: jest.fn(),
      getDraft: jest.fn(),
      uploadAudio: jest.fn(),
    };
    const save = createSaveContributorLessonUseCase(api);
    const draft = { ...emptyLessonDraft(), title: 'Greetings' };
    const result = await save(draft, 'lang-1');
    expect(result.lessonId).toBe('lesson-1');
    expect(api.createLesson).toHaveBeenCalled();
    expect(api.saveDraft).toHaveBeenCalledWith(expect.objectContaining({ lessonId: 'lesson-1', title: 'Greetings' }));
  });
});
