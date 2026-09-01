using Amazon.S3;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain;
using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Infrastructure;
using Kalanga.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Kalanga.Tests.Integration;

public sealed class KalangaWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "kalanga-integration-test-signing-key-32";

    private readonly Dictionary<string, string?> _settings;

    public KalangaWebApplicationFactory(string connectionString)
    {
        _settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSQL"] = connectionString,
            ["Jwt:Issuer"] = "kalanga-api",
            ["Jwt:Audience"] = "kalanga-clients",
            ["Jwt:SigningKey"] = SigningKey,
            ["Jwt:AccessTokenHours"] = "24",
            ["Jwt:RefreshTokenDays"] = "30",
            ["ContentPacks:SigningKey"] = SigningKey,
            ["AudioStorage:Bucket"] = "kalanga-test",
            ["AudioStorage:Region"] = "eu-west-1",
            ["AudioStorage:CdnBaseUrl"] = "https://cdn.test",
            ["AudioStorage:TimeoutSeconds"] = "5",
        };
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(_settings));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(_settings));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAmazonS3>();
            services.RemoveAll<IAudioStorage>();
            services.AddSingleton<IAudioStorage, FakeAudioStorage>();
        });
    }

    public async Task SeedKalangaLanguageAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.SeedDevelopmentDataAsync();
    }

    public async Task SeedAdminAsync(string email, string password)
    {
        await using var scope = Services.CreateAsyncScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var now = DateTimeOffset.UtcNow;
        var admin = User.Register(
            WellKnownLanguages.KalangaId,
            email,
            hasher.Hash(password),
            "Integration Admin",
            now);
        admin.ChangeRole(Role.Admin, now);
        await users.AddAsync(WellKnownLanguages.KalangaId, admin);
    }
}
