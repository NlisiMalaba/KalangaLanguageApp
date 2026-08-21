using Kalanga.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Kalanga.Api.Authorization;

public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string ContributorOrAdmin = "ContributorOrAdmin";
    public const string ReviewerOrAdmin = "ReviewerOrAdmin";

    public static IServiceCollection AddKalangaAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminOnly, policy => policy.RequireRole(nameof(Role.Admin)));
            options.AddPolicy(
                ContributorOrAdmin,
                policy => policy.RequireRole(nameof(Role.Contributor), nameof(Role.Admin)));
            options.AddPolicy(
                ReviewerOrAdmin,
                policy => policy.RequireRole(nameof(Role.Reviewer), nameof(Role.Admin)));
        });

        return services;
    }
}
