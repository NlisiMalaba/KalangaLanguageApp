export class AudioError extends Error {
  constructor(message: string) {
    super(message);
    this.name = new.target.name;
  }
}

export class AudioValidationError extends AudioError {}

export class AudioUnavailableError extends AudioError {
  constructor() {
    super('No playable audio is available for this phrase.');
  }
}

export class AudioRecordingNotFoundError extends AudioError {
  constructor(readonly recordingId: string) {
    super('That recording is not available for this phrase.');
  }
}

export class AudioPlaybackError extends AudioError {}
