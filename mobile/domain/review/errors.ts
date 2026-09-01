export class ReviewError extends Error {
  constructor(message: string) {
    super(message);
    this.name = new.target.name;
  }
}

export class ReviewApiError extends ReviewError {}
