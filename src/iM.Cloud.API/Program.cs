using BaseCrud.PrimeNg;
using iM.Cloud.API.Authorization;
using iM.Cloud.API.Services;
using iM.Cloud.API.Swagger;
using iM.Cloud.Application;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Infrastructure;
using iM.Cloud.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new FilterMetadataConverter());
    options.JsonSerializerOptions.Converters.Add(new PrimeTableMetaConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();

if (builder.Environment.IsEnvironment("NSwag"))
    builder.Services.AddNswagOpenApiDocument();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPermissionAuthorization();

var app = builder.Build();

if (!app.Environment.IsEnvironment("NSwag"))
    await DataSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", string.Empty);
        options.RoutePrefix = "swagger";
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health")
    .WithTags("Health")
    .AllowAnonymous();

app.Run();
