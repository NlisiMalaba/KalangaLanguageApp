import { Role, type Role as RoleValue } from '@/domain/enums';
import type { AuthApi, LoginApiResult, RegisterApiResult } from '@/domain/auth/ports';
import { apiRequest } from '@/utils/api';

type RegisterResponse = {
  userId: string;
  languageId: string;
  email: string;
  displayName: string;
  role: unknown;
};

type LoginResponse = RegisterResponse & {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
};

const ROLE_BY_NUMBER: RoleValue[] = [Role.Learner, Role.Contributor, Role.Reviewer, Role.Admin];

function mapRole(value: unknown): RoleValue {
  if (typeof value === 'string' && (Object.values(Role) as string[]).includes(value)) {
    return value as RoleValue;
  }

  if (typeof value === 'number' && ROLE_BY_NUMBER[value]) {
    return ROLE_BY_NUMBER[value];
  }

  return Role.Learner;
}

function mapRegister(response: RegisterResponse): RegisterApiResult {
  return {
    userId: response.userId,
    languageId: response.languageId,
    email: response.email,
    displayName: response.displayName,
    role: mapRole(response.role),
  };
}

function mapLogin(response: LoginResponse): LoginApiResult {
  return {
    id: response.userId,
    languageId: response.languageId,
    email: response.email,
    displayName: response.displayName,
    role: mapRole(response.role),
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    accessTokenExpiresAt: response.accessTokenExpiresAt,
    refreshTokenExpiresAt: response.refreshTokenExpiresAt,
  };
}

export function createHttpAuthApi(): AuthApi {
  return {
    register: async (input) => {
      const response = await apiRequest<RegisterResponse>('/auth/register', {
        method: 'POST',
        body: {
          languageId: input.languageId,
          email: input.email,
          password: input.password,
          displayName: input.displayName,
        },
      });
      return mapRegister(response);
    },
    login: async (input) => {
      const response = await apiRequest<LoginResponse>('/auth/login', {
        method: 'POST',
        body: {
          languageId: input.languageId,
          email: input.email,
          password: input.password,
        },
      });
      return mapLogin(response);
    },
  };
}
