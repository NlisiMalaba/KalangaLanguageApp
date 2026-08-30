import { ExerciseType, LessonStatus, Role } from '@/domain/enums';

describe('domain enums', () => {
  it('uses backend Role names including default Learner', () => {
    expect(Role.Learner).toBe('Learner');
    expect(Object.values(Role)).toEqual(
      expect.arrayContaining(['Learner', 'Contributor', 'Reviewer', 'Admin']),
    );
  });

  it('mirrors backend LessonStatus and ExerciseType names', () => {
    expect(LessonStatus.Published).toBe('Published');
    expect(ExerciseType.Flashcard).toBe('Flashcard');
    expect(ExerciseType.Listening).toBe('Listening');
  });
});
