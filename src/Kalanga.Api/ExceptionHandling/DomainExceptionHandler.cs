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
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            InvalidRefreshTokenException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            UnauthorizedRoleException => (StatusCodes.Status403Forbidden, "Forbidden"),
            UserSuspendedException => (StatusCodes.Status403Forbidden, "Forbidden"),
            UserNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            InvalidUserManagementException => (StatusCodes.Status400BadRequest, "Bad Request"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request"),
        };
}
