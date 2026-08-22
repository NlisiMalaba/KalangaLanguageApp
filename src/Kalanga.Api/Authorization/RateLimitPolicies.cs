using System.Security.Claims;
using System.Threading.RateLimiting;
using Kalanga.Domain;

namespace Kalanga.Api.Authorization;

public static class RateLimitPolicies
{
    public const string AudioUpload = "audio-upload";

    public static RateLimitPartition<string> AudioUploadPartition(HttpContext context)
    {
        var partition = context.User.FindFirstValue("sub")
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partition,
            static _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = DomainRules.AudioUploadMaxRequestsPerWindow,
                Window = TimeSpan.FromSeconds(DomainRules.AudioUploadRateLimitWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    }
}
