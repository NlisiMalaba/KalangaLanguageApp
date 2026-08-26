import {
  MAX_DISPLAY_NAME_LENGTH,
  MAX_EMAIL_LENGTH,
  MAX_PASSWORD_LENGTH,
  MIN_PASSWORD_LENGTH,
} from '@/constants/auth';
import { AuthValidationError } from '@/domain/auth/errors';

export function normalizeEmail(email: string): string {
  return email.trim().toLowerCase();
}

export function requireLanguageId(languageId: string): string {
  const id = languageId.trim();
  if (id.length === 0) {
    throw new AuthValidationError('language_id is required.');
  }

  return id;
}

export function requireEmail(email: string): string {
  const normalized = normalizeEmail(email);
  if (normalized.length === 0 || normalized.length > MAX_EMAIL_LENGTH || !normalized.includes('@')) {
    throw new AuthValidationError('A valid email is required.');
  }

  return normalized;
}

export function requirePassword(password: string, { requireMinLength }: { requireMinLength: boolean }): string {
  if (password.length === 0 || password.length > MAX_PASSWORD_LENGTH) {
    throw new AuthValidationError('A valid password is required.');
  }

  if (requireMinLength && password.length < MIN_PASSWORD_LENGTH) {
    throw new AuthValidationError(`Password must be at least ${MIN_PASSWORD_LENGTH} characters.`);
  }

  return password;
}

export function requireDisplayName(displayName: string): string {
  const name = displayName.trim();
  if (name.length === 0 || name.length > MAX_DISPLAY_NAME_LENGTH) {
    throw new AuthValidationError('A display name is required.');
  }

  return name;
}
