import { createBcryptPasswordHasher } from '@/lib/auth/bcryptPasswordHasher';

describe('createBcryptPasswordHasher', () => {
  const hasher = createBcryptPasswordHasher(4);

  it('hashes a password and verifies it', async () => {
    const hash = await hasher.hash('password1');
    expect(hash.startsWith('$2')).toBe(true);
    await expect(hasher.verify('password1', hash)).resolves.toBe(true);
    await expect(hasher.verify('wrong-pass', hash)).resolves.toBe(false);
  });
});
