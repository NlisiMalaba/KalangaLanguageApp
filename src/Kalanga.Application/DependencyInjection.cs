using FluentValidation;
using Kalanga.Application.Ports.In;
using Kalanga.Application.UseCases;
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
        services.AddScoped<BrowseLessonCatalogPort, BrowseLessonCatalogUseCase>();
        services.AddScoped<GetLessonPort, GetLessonUseCase>();
        services.AddScoped<CreateLessonPort, CreateLessonUseCase>();
        services.AddScoped<SaveLessonDraftPort, SaveLessonDraftUseCase>();
        services.AddScoped<SubmitLessonForReviewPort, SubmitLessonForReviewUseCase>();
        return services;
    }
}
