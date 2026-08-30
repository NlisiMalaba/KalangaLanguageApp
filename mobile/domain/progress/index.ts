export { createCompleteLessonUseCase } from './completeLessonUseCase';
export type { CompleteLessonDeps, CompleteLessonInput, CompleteLessonResult, EnqueueSyncItem } from './completeLessonUseCase';
export { InvalidLessonCompletionError, ProgressError } from './errors';
export { awardXp, createLearnerGamification, recordActivity } from './gamification';
