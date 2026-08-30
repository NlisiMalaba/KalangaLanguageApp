export class SyncError extends Error {
  constructor(message: string) {
    super(message);
    this.name = new.target.name;
  }
}

export class DuplicateSyncPushError extends SyncError {
  constructor() {
    super('Duplicate sync push.');
  }
}
