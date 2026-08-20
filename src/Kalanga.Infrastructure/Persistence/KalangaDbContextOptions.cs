using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence;

internal static class KalangaDbContextOptions
{
    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(KalangaDbContext).Assembly.GetName().Name);
                npgsql.CommandTimeout(30);
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        options.UseSnakeCaseNamingConvention();
    }
}
