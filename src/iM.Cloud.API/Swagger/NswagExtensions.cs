using NSwag;
using NSwag.Generation.Processors.Security;

namespace iM.Cloud.API.Swagger;

public static class NswagExtensions
{
    public static IServiceCollection AddNswagOpenApiDocument(this IServiceCollection services)
    {
        services.AddOpenApiDocument(document =>
        {
            document.DocumentName = "v1";
            document.Title = "iM.Cloud API";
            document.Version = "v1";

            document.AddSecurity("Bearer", [], new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Name = "Authorization",
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });

            document.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
        });

        return services;
    }
}
