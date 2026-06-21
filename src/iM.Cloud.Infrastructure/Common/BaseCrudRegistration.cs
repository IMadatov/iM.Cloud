using System.Reflection;
using BaseCrud;
using BaseCrud.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace iM.Cloud.Infrastructure.Common;

public static class BaseCrudRegistration
{
    public static IServiceCollection AddBaseCrud(this IServiceCollection services)
    {
        services.AddBaseCrudService(new BaseCrudServiceOptions
        {
            Assemblies =
            [
                typeof(BaseCrudRegistration).Assembly,
                typeof(iM.Cloud.Application.DependencyInjection).Assembly,
                typeof(iM.Cloud.Domain.Entities.Group).Assembly
            ]
        });

        return services;
    }
}
