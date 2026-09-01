import { ContributorValidationError } from '@/domain/contributor/errors';
import { encodeExercisePrompt } from '@/domain/contributor/encodeExercisePrompt';
import type { ContributorLessonApi, CreatedLesson, LessonDraft } from '@/domain/contributor/types';
import { assertLessonDraftValid } from '@/domain/contributor/validateLessonDraft';
import { LessonStatus } from '@/domain/enums';

export function createSaveContributorLessonUseCase(api: ContributorLessonApi) {
  return async function saveContributorLesson(draft: LessonDraft, languageId: string): Promise<CreatedLesson> {
    assertLessonDraftValid(draft);

    const scenarioContext = draft.isScenario ? draft.scenarioContext.trim() : null;
    const createInput = {
      languageId,
      title: draft.title.trim(),
      level: draft.level,
      category: draft.category.trim(),
      isScenario: draft.isScenario,
      scenarioContext,
      xpReward: draft.xpReward,
    };

    const lessonId = draft.id
      ? draft.id
      : (await api.createLesson(createInput)).lessonId;

    await api.saveDraft({
      ...createInput,
      lessonId,
      phrases: draft.phrases,
      exercises: draft.exercises,
    });

    return { lessonId, status: draft.status ?? LessonStatus.Draft };
  };
}

export function createSubmitContributorLessonUseCase(api: ContributorLessonApi) {
  return async function submitContributorLesson(draft: LessonDraft, languageId: string) {
    if (!draft.id) {
      throw new ContributorValidationError('Save the draft before submitting it for review.');
    }

    assertLessonDraftValid(draft);
    if (draft.phrases.length === 0 || draft.exercises.length === 0) {
      throw new ContributorValidationError('Add at least one phrase and one exercise before submitting.');
    }

    return api.submitForReview(languageId, draft.id);
  };
}

export function toDraftExercisePayload(exercise: LessonDraft['exercises'][number]) {
  return encodeExercisePrompt(exercise);
}
