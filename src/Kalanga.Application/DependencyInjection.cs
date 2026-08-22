using FluentValidation;
using Kalanga.Application.Ports.In;
using Kalanga.Application.UseCases;
using Kalanga.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kalanga.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<RegisterUserPort, RegisterUserUseCase>();
        services.AddScoped<AuthenticateUserPort, AuthenticateUserUseCase>();
        services.AddScoped<RefreshTokenPort, RefreshTokenUseCase>();
        services.AddScoped<LogoutPort, LogoutUseCase>();
        services.AddScoped<ManageUserRolePort, ManageUserRoleUseCase>();
        services.AddScoped<ListUsersPort, ListUsersUseCase>();
        services.AddScoped<GetPlatformMetricsPort, GetPlatformMetricsUseCase>();
        services.AddScoped<BrowseLessonCatalogUseCase>();
        services.AddScoped<BrowseLessonCatalogPort, CachingBrowseLessonCatalogUseCase>();
        services.AddScoped<GetLessonPort, GetLessonUseCase>();
        services.AddScoped<CreateLessonPort, CreateLessonUseCase>();
        services.AddScoped<SaveLessonDraftPort, SaveLessonDraftUseCase>();
        services.AddScoped<SubmitLessonForReviewPort, SubmitLessonForReviewUseCase>();
        services.AddScoped<ListReviewQueuePort, ListReviewQueueUseCase>();
        services.AddScoped<ReviewLessonPort, ReviewLessonUseCase>();
        services.AddScoped<OverrideLessonPublicationPort, OverrideLessonPublicationUseCase>();
        services.AddScoped<UploadAudioPort, UploadAudioUseCase>();
        services.AddScoped<GetAudioPort, GetAudioUseCase>();
        services.AddScoped<ListContentPacksPort, ListContentPacksUseCase>();
        services.AddScoped<GetContentPackManifestPort, GetContentPackManifestUseCase>();
        services.AddScoped<CompleteLessonPort, CompleteLessonUseCase>();
        services.AddScoped<GetProgressPort, GetProgressUseCase>();
        services.AddScoped<UpdateStreakPort, UpdateStreakUseCase>();
        services.AddScoped<AdvanceLevelPort, AdvanceLevelUseCase>();
        services.AddSingleton<SpacedRepetitionService>();
        return services;
    }
}
