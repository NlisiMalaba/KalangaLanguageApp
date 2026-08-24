import type { User } from '@/domain/entities';
import { Role, UserStatus } from '@/domain/enums';

describe('domain entities', () => {
  it('describes a User without a password hash field', () => {
    const user: User = {
      id: '11111111-1111-7111-8111-111111111111',
      languageId: '22222222-2222-7222-8222-222222222222',
      email: 'learner@example.test',
      displayName: 'Learner',
      role: Role.Learner,
      status: UserStatus.Active,
      createdAt: '2026-08-23T12:00:00.000Z',
      updatedAt: '2026-08-23T12:00:00.000Z',
    };

    expect(user.role).toBe(Role.Learner);
    expect(user).not.toHaveProperty('passwordHash');
  });
});
