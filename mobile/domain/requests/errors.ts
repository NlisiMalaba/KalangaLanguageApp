export class RequestError extends Error {
  constructor(message: string) {
    super(message);
    this.name = new.target.name;
  }
}

export class RequestValidationError extends RequestError {}

export class RequestApiError extends RequestError {}
