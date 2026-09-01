export class AuthError extends Error {
  constructor(message: string) {
    super(message);
    this.name = new.target.name;
    Object.setPrototypeOf(this, new.target.prototype);
  }
}

export class AuthValidationError extends AuthError {}

export class InvalidCredentialsError extends AuthError {
  constructor() {
    super('Invalid email or password.');
  }
}

export class UserSuspendedError extends AuthError {
  constructor() {
    super('This account is suspended.');
  }
}

export class DuplicateEmailError extends AuthError {
  constructor(readonly email: string) {
    super('An account with this email already exists.');
  }
}

export class NetworkRequiredError extends AuthError {
  constructor(action: string) {
    super(`${action} requires a network connection.`);
  }
}

export class SessionExpiredError extends AuthError {
  constructor() {
    super('Your session has expired. Please sign in again.');
  }
}

export class AuthApiError extends AuthError {
  constructor(
    readonly status: number,
    message: string,
  ) {
    super(message);
  }
}
