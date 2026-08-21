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
        services.AddScoped<ManageUserRolePort, ManageUserRoleUseCase>();
        return services;
    }
}
