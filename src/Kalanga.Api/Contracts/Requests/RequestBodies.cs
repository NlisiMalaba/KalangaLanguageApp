namespace Kalanga.Api.Contracts.Requests;

public sealed record SubmitRequestBody(string Title, string Description);

public sealed record FulfillRequestBody(Guid LessonId);
