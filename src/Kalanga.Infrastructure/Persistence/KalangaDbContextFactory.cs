using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kalanga.Infrastructure.Persistence;

public sealed class KalangaDbContextFactory : IDesignTimeDbContextFactory<KalangaDbContext>
{
    public KalangaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("KALANGA_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=kalanga;Username=kalanga;Password=kalanga;Maximum Pool Size=5;Timeout=15";

        var options = new DbContextOptionsBuilder<KalangaDbContext>();
        KalangaDbContextOptions.Configure(options, connectionString);
        return new KalangaDbContext(options.Options);
    }
}
