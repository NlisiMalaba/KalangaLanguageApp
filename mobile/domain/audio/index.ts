export { createAudioPlayer } from './audioPlayer';
export type { AudioPlayer } from './audioPlayer';
export { AudioError, AudioPlaybackError, AudioRecordingNotFoundError, AudioUnavailableError } from './errors';
export { localAudioFileUri, localAudioRelativePath } from './localAudioPath';
export { recordingsForPhrase } from './phraseRecordings';
export { resolveAudioSource } from './resolveAudioSource';
export { AudioSourceKind, PlaybackRate } from './types';
export type { AudioPlaybackSession, ResolvedAudioSource } from './types';
