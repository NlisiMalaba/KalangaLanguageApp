using Kalanga.Application.Dtos;
using Kalanga.Domain.Entities;

namespace Kalanga.Application;

internal static class RequestMapping
{
    public static RequestDto ToDto(this Request request) =>
        new(
            request.Id,
            request.LanguageId,
            request.SubmitterId,
            request.Title,
            request.Description,
            request.UpvoteCount,
            request.Status,
            request.FulfilledByLessonId,
            request.CreatedAt,
            request.UpdatedAt);
}
