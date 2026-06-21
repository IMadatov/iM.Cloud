using iM.Cloud.Domain.Authorization;
using iM.Cloud.Domain.Entities;
using iM.Cloud.Infrastructure.Identity;
using iM.Cloud.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace iM.Cloud.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public const string AdminRoleName = "Admin";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        await db.Database.MigrateAsync();

        await SeedPermissionsAsync(db);
        var adminRole = await SeedAdminRoleAsync(db, roleManager);
        await SeedBootstrapAdminAsync(userManager, configuration, logger, adminRole);
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext db)
    {
        var existing = await db.Permissions.Select(p => p.Code).ToListAsync();
        var toAdd = PermissionCodes.All
            .Where(code => !existing.Contains(code))
            .Select(code => Permission.Create(code, FormatPermissionName(code), $"Permission: {code}"))
            .ToList();

        if (toAdd.Count > 0)
        {
            db.Permissions.AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationRole> SeedAdminRoleAsync(ApplicationDbContext db, RoleManager<ApplicationRole> roleManager)
    {
        var adminRole = await roleManager.FindByNameAsync(AdminRoleName);
        if (adminRole is null)
        {
            adminRole = new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = AdminRoleName,
                NormalizedName = AdminRoleName.ToUpperInvariant(),
                Description = "System administrator",
                Active = true,
                CreatedDate = DateTime.UtcNow
            };
            await roleManager.CreateAsync(adminRole);
        }

        var allPermissionIds = await db.Permissions.Select(p => p.Id).ToListAsync();
        var assigned = await db.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        foreach (var permissionId in allPermissionIds.Except(assigned))
            db.RolePermissions.Add(RolePermission.Create(adminRole.Id, permissionId));

        await db.SaveChangesAsync();
        return adminRole;
    }

    private static async Task SeedBootstrapAdminAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger,
        ApplicationRole adminRole)
    {
        var email = configuration["Bootstrap:AdminEmail"] ?? configuration["Bootstrap__AdminEmail"];
        var password = configuration["Bootstrap:AdminPassword"] ?? configuration["Bootstrap__AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Bootstrap admin credentials not configured. Skipping admin user seed.");
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email.Trim(),
                Email = email.Trim(),
                DisplayName = "Administrator",
                IsActive = true,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create bootstrap admin: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, adminRole.Name!))
            await userManager.AddToRoleAsync(user, adminRole.Name!);
    }

    private static string FormatPermissionName(string code) =>
        string.Join(' ', code.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpper(part[0]) + part[1..]));
}
