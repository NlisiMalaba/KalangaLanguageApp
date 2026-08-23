using Amazon;
using Amazon.S3;
using Kalanga.Application.Ports.Out;
using Kalanga.Infrastructure.Caching;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Repositories;
using Kalanga.Infrastructure.Security;
using Kalanga.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kalanga.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("Connection string 'PostgreSQL' is not configured.");

        services.AddDbContext<KalangaDbContext>(options =>
            KalangaDbContextOptions.Configure(options, connectionString));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<ContentPacksOptions>(configuration.GetSection(ContentPacksOptions.SectionName));
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IContentPackManifestSigner, HmacContentPackManifestSigner>();

        services.AddSingleton<ICatalogCache, MemoryCatalogCache>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPlatformMetricsReader, PlatformMetricsReader>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IPhraseRepository, PhraseRepository>();
        services.AddScoped<ILanguageVariationRepository, LanguageVariationRepository>();
        services.AddScoped<IAudioRecordingRepository, AudioRecordingRepository>();
        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IContentPackRepository, ContentPackRepository>();
        services.AddScoped<ILearnerProgressRepository, LearnerProgressRepository>();
        services.AddScoped<ISpacedRepetitionRepository, SpacedRepetitionRepository>();
        services.AddScoped<IGamificationRepository, GamificationRepository>();
        services.AddScoped<IRequestRepository, RequestRepository>();
        services.AddScoped<ISyncCheckpointRepository, SyncCheckpointRepository>();
        services.AddScoped<ISyncPushReceiptRepository, SyncPushReceiptRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<INotificationOutbox, NotificationOutbox>();

        services.Configure<AudioStorageOptions>(configuration.GetSection(AudioStorageOptions.SectionName));
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<AudioStorageOptions>>().Value;
            var region = string.IsNullOrWhiteSpace(settings.Region) ? "eu-west-1" : settings.Region;
            return new AmazonS3Client(new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(region),
                Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)),
            });
        });
        services.AddSingleton<S3AudioStorage>();
        services.AddSingleton<IAudioStorage, ResilientAudioStorage>();

        return services;
    }
}
