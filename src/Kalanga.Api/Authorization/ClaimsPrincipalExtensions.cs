using System.Security.Claims;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Api.Authorization;

public static class ClaimsPrincipalExtensions
{
    public const string LanguageIdClaim = "language_id";

    public static UserId GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Authenticated user is missing a subject claim.");

        if (!Guid.TryParse(raw, out var id))
        {
            throw new InvalidOperationException("Authenticated user subject claim is not a valid user id.");
        }

        return UserId.From(id);
    }

    public static LanguageId GetLanguageId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(LanguageIdClaim)
            ?? throw new InvalidOperationException("Authenticated user is missing a language_id claim.");

        if (!Guid.TryParse(raw, out var id))
        {
            throw new InvalidOperationException("Authenticated user language_id claim is not a valid language id.");
        }

        return LanguageId.From(id);
    }
}
