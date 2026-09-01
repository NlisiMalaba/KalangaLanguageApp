using System.Diagnostics;
using Kalanga.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.ExceptionHandling;

public sealed class DomainExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        var (statusCode, title) = Map(domainException);
        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = domainException.Message,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}",
        };

        problemDetails.Extensions["correlation_id"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (domainException is DuplicateEmailException duplicate)
        {
            problemDetails.Extensions["email"] = duplicate.Email;
        }

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });

        return true;
    }

    private static (int StatusCode, string Title) Map(DomainException exception) =>
        exception switch
        {
            DuplicateEmailException => (StatusCodes.Status409Conflict, "Conflict"),
            DuplicateLanguageCodeException => (StatusCodes.Status409Conflict, "Conflict"),
            LanguageNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            DuplicateLearnerProgressException => (StatusCodes.Status409Conflict, "Conflict"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            InvalidRefreshTokenException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            UnauthorizedRoleException => (StatusCodes.Status403Forbidden, "Forbidden"),
            UserSuspendedException => (StatusCodes.Status403Forbidden, "Forbidden"),
            UserNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            LessonNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            RequestNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ContentPackNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            TenantAccessDeniedException => (StatusCodes.Status403Forbidden, "Forbidden"),
            LessonAccessDeniedException => (StatusCodes.Status403Forbidden, "Forbidden"),
            InvalidRequestStateException => (StatusCodes.Status409Conflict, "Conflict"),
            InvalidUserManagementException => (StatusCodes.Status400BadRequest, "Bad Request"),
            InvalidReviewException => (StatusCodes.Status400BadRequest, "Bad Request"),
            InvalidLessonCompletionException => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
            InvalidSyncPayloadException => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
            DuplicateSyncPushException => (StatusCodes.Status409Conflict, "Conflict"),
            InvalidAudioUploadException => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
            InvalidAudioRecordingException => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
            PhraseNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ExerciseNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            AudioRecordingNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            VariationNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            AudioStorageUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Service Unavailable"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request"),
        };
}
