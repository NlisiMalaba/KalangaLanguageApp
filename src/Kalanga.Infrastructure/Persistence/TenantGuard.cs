using Kalanga.Domain.ValueObjects;

namespace Kalanga.Infrastructure.Persistence;

internal static class TenantGuard
{
    public const int MaxPageSize = 100;

    public static void Ensure(LanguageId expected, LanguageId actual)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException("Entity language_id does not match the repository tenant.");
        }
    }

    public static int ClampTake(int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);
        return Math.Min(take, MaxPageSize);
    }

    public static void EnsureSkip(int skip) => ArgumentOutOfRangeException.ThrowIfNegative(skip);
}
