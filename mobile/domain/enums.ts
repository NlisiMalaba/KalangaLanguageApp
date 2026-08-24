/**
 * String values match C# enum.ToString() as stored by EF (varchar) and JWT role claims.
 */

export const Role = {
  Learner: 'Learner',
  Contributor: 'Contributor',
  Reviewer: 'Reviewer',
  Admin: 'Admin',
} as const;
export type Role = (typeof Role)[keyof typeof Role];

export const UserStatus = {
  Active: 'Active',
  Suspended: 'Suspended',
} as const;
export type UserStatus = (typeof UserStatus)[keyof typeof UserStatus];

export const Level = {
  Beginner: 'Beginner',
  Intermediate: 'Intermediate',
  Advanced: 'Advanced',
} as const;
export type Level = (typeof Level)[keyof typeof Level];

export const LessonStatus = {
  Draft: 'Draft',
  PendingReview: 'PendingReview',
  Published: 'Published',
  Unpublished: 'Unpublished',
} as const;
export type LessonStatus = (typeof LessonStatus)[keyof typeof LessonStatus];

export const ExerciseType = {
  Flashcard: 'Flashcard',
  MultipleChoice: 'MultipleChoice',
  SentenceBuilder: 'SentenceBuilder',
  Listening: 'Listening',
} as const;
export type ExerciseType = (typeof ExerciseType)[keyof typeof ExerciseType];

export const AudioFileFormat = {
  Mp3: 'Mp3',
  Aac: 'Aac',
} as const;
export type AudioFileFormat = (typeof AudioFileFormat)[keyof typeof AudioFileFormat];

export const SpeakerGender = {
  Unspecified: 'Unspecified',
  Male: 'Male',
  Female: 'Female',
} as const;
export type SpeakerGender = (typeof SpeakerGender)[keyof typeof SpeakerGender];

export const AudioRecordingStatus = {
  PendingReview: 'PendingReview',
  Approved: 'Approved',
  Rejected: 'Rejected',
} as const;
export type AudioRecordingStatus =
  (typeof AudioRecordingStatus)[keyof typeof AudioRecordingStatus];

export const RequestStatus = {
  Open: 'Open',
  Fulfilled: 'Fulfilled',
} as const;
export type RequestStatus = (typeof RequestStatus)[keyof typeof RequestStatus];
