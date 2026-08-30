export class PronunciationError extends Error {
  constructor(message: string) {
    super(message);
    this.name = new.target.name;
  }
}

export class MicrophonePermissionDeniedError extends PronunciationError {
  constructor() {
    super('Microphone access is required for pronunciation practice.');
  }
}

export class PronunciationRecordingError extends PronunciationError {}

export class PronunciationConsentRequiredError extends PronunciationError {
  constructor() {
    super('Pronunciation recordings stay on this device unless you consent to upload.');
  }
}
