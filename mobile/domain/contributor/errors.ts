export class ContributorError extends Error {
  constructor(message: string) {
    super(message);
    this.name = new.target.name;
  }
}

export class ContributorValidationError extends ContributorError {}

export class ContributorApiError extends ContributorError {}
