using iM.Cloud.Application.Auth;
using iM.Cloud.Application.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace iM.Cloud.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<GetMeHandler>();
        services.AddScoped<GetMyNavigationHandler>();

        return services;
    }
}
