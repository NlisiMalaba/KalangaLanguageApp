export class ContentPackError extends Error {
  constructor(message: string) {
    super(message);
    this.name = new.target.name;
  }
}

export class ContentPackApiError extends ContentPackError {}

export class ContentPackNotFoundError extends ContentPackError {
  constructor(readonly packId: string) {
    super('This content pack is not available.');
  }
}

export class InsufficientStorageError extends ContentPackError {
  constructor() {
    super('Download paused. Less than 50 MB of storage is free.');
  }
}

export class ContentPackDownloadError extends ContentPackError {
  constructor(message: string) {
    super(message);
  }
}
