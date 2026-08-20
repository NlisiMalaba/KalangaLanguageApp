using Kalanga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Kalanga.Tests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("kalanga")
            .WithUsername("kalanga")
            .WithPassword("kalanga")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public KalangaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<KalangaDbContext>();
        KalangaDbContextOptions.Configure(options, ConnectionString, enableRetryOnFailure: false);
        return new KalangaDbContext(options.Options);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
