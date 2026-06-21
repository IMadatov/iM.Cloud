using Microsoft.OpenApi;

namespace iM.Cloud.API.Swagger;

public static class SwaggerExtensions
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "iM.Cloud API",
                Version = "v1"
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "JWT token. Example: eyJhbGciOiJIUzI1NiIs...",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document)] = []
            });
        });

        return services;
    }
}
