using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence;

public sealed class KalangaDbContext(DbContextOptions<KalangaDbContext> options) : DbContext(options)
{
}
