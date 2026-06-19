using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace iM.Cloud.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Auth fazasida EF Core, JWT, BCrypt shu yerda
        return services;
    }
}
