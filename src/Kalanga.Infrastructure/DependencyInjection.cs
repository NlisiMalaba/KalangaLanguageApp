using Kalanga.Application.Ports.Out;
using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IPhraseRepository, PhraseRepository>();
        services.AddScoped<IAudioRecordingRepository, AudioRecordingRepository>();
        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IContentPackRepository, ContentPackRepository>();
        services.AddScoped<ILearnerProgressRepository, LearnerProgressRepository>();
        services.AddScoped<ISpacedRepetitionRepository, SpacedRepetitionRepository>();
        services.AddScoped<IGamificationRepository, GamificationRepository>();
        services.AddScoped<IRequestRepository, RequestRepository>();
        services.AddScoped<ISyncCheckpointRepository, SyncCheckpointRepository>();

        return services;
    }
}
