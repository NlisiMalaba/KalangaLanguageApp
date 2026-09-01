import type { EntityId, Instant, Request } from '@/domain/entities';
import type { RequestStatus } from '@/domain/enums';

export type CommunityRequest = Request;

export type SubmitRequestInput = {
  languageId: EntityId;
  title: string;
  description: string;
};

export type UpvoteRequestInput = {
  languageId: EntityId;
  requestId: EntityId;
};

export type FulfillRequestInput = {
  languageId: EntityId;
  requestId: EntityId;
  lessonId: EntityId;
};

export type UpvoteRequestResult = {
  request: CommunityRequest;
  applied: boolean;
};

export type ListRequestsInput = {
  languageId: EntityId;
  skip?: number;
  take?: number;
};

export type RequestsApi = {
  list: (input: ListRequestsInput) => Promise<CommunityRequest[]>;
  submit: (input: SubmitRequestInput) => Promise<CommunityRequest>;
  upvote: (input: UpvoteRequestInput) => Promise<UpvoteRequestResult>;
  fulfill: (input: FulfillRequestInput) => Promise<CommunityRequest>;
};

export type MappedRequestStatus = RequestStatus;
export type RequestInstant = Instant;
