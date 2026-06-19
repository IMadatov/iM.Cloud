using Microsoft.Extensions.DependencyInjection;

namespace iM.Cloud.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth fazasida handler'lar shu yerda ro'yxatdan o'tadi
        return services;
    }
}
